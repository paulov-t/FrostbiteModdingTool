using FMT.FileTools;
using FMT.Models;
using FrostbiteModdingUI.Windows;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FIFAModdingUI.Pages.Common
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty AssetEntryProperty;

        public static readonly DependencyProperty AssetModifiedProperty;

        protected IEditorWindow EditorWindow;

        protected List<object> objects;

        protected List<object> rootObjects;

        protected Dictionary<Guid, EbxAsset> dependentObjects = new Dictionary<Guid, EbxAsset>();

        protected EbxAsset asset;

        public object RootObject { get { return asset.RootObject; } }

        private List<ModdableProperty> _rootObjProps;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Item 1: PropertyName
        /// Item 2: PropertyType
        /// Item 3: PropertyValue
        /// </summary>
        public List<ModdableProperty> RootObjectProperties
        {
            get
            {
                if (_rootObjProps == null)
                {
                    _rootObjProps = new List<ModdableProperty>();
                    if (RootObject != null)
                    {
                        var vanillaRootObject = AssetManager.Instance.GetEbx((EbxAssetEntry)AssetEntry, false).RootObject;

                        _rootObjProps = ModdableProperty.GetModdableProperties(RootObject, Modprop_PropertyChanged, vanillaRootObject).ToList();
                        return _rootObjProps
                            .OrderBy(x => x.PropertyName == "BaseField")
                            .ThenBy(x => x.PropertyName == "Name")
                            .ThenBy(x => x.PropertyName).ToList();
                    }
                }
                return _rootObjProps;
            }
            set
            {
                _rootObjProps = value;
                foreach (var item in _rootObjProps.Where(x => !x.PropertyName.StartsWith("__")))
                {
                    Utilities.SetPropertyValue(RootObject, item.PropertyName, item.PropertyValue);
                }

                AssetManager.Instance.ModifyEbx(AssetEntry.Name, asset);
            }
        }

        private void Modprop_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var index = _rootObjProps.FindIndex(x => x.PropertyName == ((ModdableProperty)sender).PropertyName);
            var o = _rootObjProps[index];
            o.PropertyValue = ((ModdableProperty)sender).PropertyValue;
            var robjs = RootObjectProperties;
            robjs[index] = o;
            RootObjectProperties = robjs;

            //if(EditorWindow != null)
            //	EditorWindow.UpdateAllBrowsers();

            if (EditorWindow != null)
                EditorWindow.Log($"[{DateTime.Now.ToString("t")}] {asset.RootObject} Saved");
        }

        public void RevertAsset()
        {
            AssetManager.Instance.RevertAsset(AssetEntry);
            this.Visibility = Visibility.Collapsed;
        }

        public IEnumerable<object> RootObjects => asset.RootObjects;

        public IEnumerable<object> Objects => asset.Objects;

        public IAssetEntry AssetEntry { get; set; }

        //public EbxAsset Asset { get { return asset; } set { asset = value; if(PropertyChanged != null) PropertyChanged.Invoke(this, null); } }
        public EbxAsset Asset { get { return asset; } set { asset = value; } }

        [Obsolete("Incorrect usage of Editor Windows")]

        public Editor()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        //[Deprecated("This is only used for Testing Purposes", DeprecationType.Deprecate, 1)]
        public Editor(EbxAsset ebx
            )
        {
            InitializeComponent();
            PropertyChanged += Editor_PropertyChanged;
            // intialise objs
            Asset = ebx;
            this.DataContext = this;
            this.TreeView1.DataContext = RootObject;
            //this.TreeViewOriginal.DataContext = RootObject;
            //this.TreeViewOriginal.IsEnabled = false;
        }

        public static Editor CurrentEditorInstance { get; set; }

        //public Editor(AssetEntry inAssetEntry
        //	, EbxAsset inAsset
        //	, FrostbiteProject frostyProject
        //	, IEditorWindow inEditorWindow)
        //{
        //	InitializeComponent();
        //	CurrentEditorInstance = this;
        //	PropertyChanged += Editor_PropertyChanged;
        //	LoadEbx(inAssetEntry, inAsset, frostyProject, inEditorWindow);

        //}

        public async Task<bool> LoadEbx(
            IAssetEntry inAssetEntry
            , EbxAsset inAsset
            , IEditorWindow inEditorWindow)
        {

            if (inAssetEntry == null)
                return false;

            if (inAsset == null)
            {
                FileLogger.WriteLine($"Unable to load Editor for {inAssetEntry.Name} as Ebx Asset is NULL");
                return false;
            }

            CurrentEditorInstance = this;

            _rootObjProps = null;
            PropertyChanged += Editor_PropertyChanged;
            AssetEntry = inAssetEntry;
            Asset = inAsset;
            EditorWindow = inEditorWindow;

            bool success = true;

            await Dispatcher.InvokeAsync(() =>
            {
                success = CreateEditor(RootObjectProperties, TreeView1).Result;
                this.DataContext = null;
                this.DataContext = this;
            });

            return success;
        }

        private void Editor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        public Control GetMatchingTypedControl(object d)
        {
            var propertyNSearch = d.GetType().ToString().ToLower().Replace(".", "_", StringComparison.OrdinalIgnoreCase);

            if (UsableTypes == null)
                UsableTypes = Assembly.GetExecutingAssembly().GetTypes();

            var ty = UsableTypes.FirstOrDefault(x => x.Name.Contains(propertyNSearch, StringComparison.OrdinalIgnoreCase));

            if (ty != null)
            {
                Control control = Activator.CreateInstance(ty) as Control;
                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }

        public Control GetMatchingTypedControl(ModdableProperty moddableProperty)
        {
            var propertyNSearch = moddableProperty.PropertyType.Replace(".", "_", StringComparison.OrdinalIgnoreCase);

            if (UsableTypes == null)
                UsableTypes = Assembly.GetExecutingAssembly().GetTypes();

            var ty = UsableTypes.FirstOrDefault(x => x.Name.Contains(propertyNSearch, StringComparison.OrdinalIgnoreCase));

            if (ty != null)
            {
                Control control = Activator.CreateInstance(ty) as Control;
                if (control != null)
                {
                    return control;
                }
            }

            return null;
        }


        public static IEnumerable<Type> UsableTypes { get; private set; }

        public async Task<bool> CreateEditor(ModdableProperty d, TreeView treeView)
        {
            Control control = GetMatchingTypedControl(d);
            if (control != null)
            {
                control.DataContext = null;
                control.DataContext = d;
                treeView.Items.Add(control);
                return true;
            }

            //if (d is ModdableProperty)
            //{
            //    var mp = d as ModdableProperty;

            //    if (IsList(mp.PropertyValue))
            //    {
            //        var lst = mp.PropertyValue as IList;
            //        foreach (var item in lst)
            //        {
            //            await CreateEditor(item, treeView);
            //        }
            //    }
            //    else
            //    {
            //        await CreateEditor(d, treeView);
            //    }
            //}

            Debug.WriteLine($"ERROR: Failed to create EBX Editor for {d.GetType()}");
            Console.WriteLine($"ERROR: Failed to create EBX Editor for {d.GetType()}");
            EditorWindow.LogError($"ERROR: Failed to create EBX Editor for {d.GetType()}");

            return false;
        }

        public bool CreateEditor(ModdableProperty d, TreeViewItem treeViewItem, TreeView treeView = null)
        {
            if (treeViewItem.ToolTip == null)
                treeViewItem.ToolTip = d.PropertyDescription;

            Control control = GetMatchingTypedControl(d);
            if (control != null)
            {
                control.ToolTip = d.PropertyDescription;
                control.DataContext = d;
                treeViewItem.Items.Add(control);
                return true;
            }

            if (CreateEditorByList(d, treeViewItem, treeView))
                return true;

            switch (d.PropertyType)
            {
                case "FrostySdk.Ebx.PointerRef":
                    CreatePointerRefControl(d, ref treeViewItem);
                    break;
                // Unknown/Other Struct
                default:
                    var structProperties = ModdableProperty.GetModdableProperties(d.PropertyValue, (s, n) =>
                    {
                        _ = SaveToRootObject();
                    }).ToList();
                    TreeViewItem structTreeView = new TreeViewItem();
                    structTreeView.ToolTip = d.PropertyDescription;
                    structTreeView.Header = d.PropertyName;
                    foreach (var property in structProperties)
                    {
                        _ = CreateEditor(property, structTreeView);
                    }
                    treeViewItem.ToolTip = d.PropertyDescription;
                    treeViewItem.Items.Add(structTreeView);
                    break;
            }

            return false;
        }


        public bool IsList(object o)
        {
            return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public bool IsDictionary(object o)
        {
            return o is IDictionary &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<object, object>));
        }

        public bool CreateEditorByList(ModdableProperty p, TreeViewItem propTreeViewParent, TreeView treeView = null)
        {
            // Is a list / array property
            if (p.PropertyType.Contains("List`1"))
            {

                // get count in array
                var countOfArray = (int)p.PropertyValue.GetPropertyValue("Count");
                // add buttons for array...
                // get items in array
                for (var i = 0; i < countOfArray; i++)
                {
                    var itemOfArray = ((IList)p.PropertyValue)[i];
                    CreateEditor(new ModdableProperty(rootObject: p.RootObject, property: p.Property, arrayIndex: i
                        , async (moddableProperty, v) =>
                        {

                            /// --------------------
                            // TODO: Check to see if this is over kill. I think this can be handled by ModdableProperty
                            var mp = (ModdableProperty)moddableProperty;

                            IList sourceList = (IList)mp.Property.GetValue(mp.RootObject);
                            Type t = typeof(List<>).MakeGenericType(mp.ArrayType);
                            IList res = (IList)Activator.CreateInstance(t);
                            foreach (var item in sourceList)
                            {
                                res.Add(item);
                            }
                            res.RemoveAt(mp.ArrayIndex.Value);
                            res.GetType().GetMethod("Insert")
                            .Invoke(res, new object[2] { mp.ArrayIndex.Value, Convert.ChangeType(v.PropertyName, mp.ArrayType) });

                            mp.Property.SetValue(mp.RootObject, res);
                            await SaveToRootObject();
                            /// --------------------
                        }
                        , p.VanillaRootObject)
                        , propTreeViewParent
                        , treeView);
                }

                if (treeView != null)
                    treeView.Items.Add(propTreeViewParent);

                return true;
            }

            return false;
        }

        public async Task<bool> CreateEditor(List<ModdableProperty> moddableProperties, TreeView treeView)
        {
            bool success = true;

            treeView.Items.Clear();
            foreach (var p in moddableProperties)
            {
                TreeViewItem propTreeViewParent = new TreeViewItem();

                //await Dispatcher.InvokeAsync(() =>
                //{
                propTreeViewParent.Header = p.PropertyName;
                //});

                bool AddToPropTreeViewParent = true;

                // Attempt to use the prebuilt controls
                Control control = GetMatchingTypedControl(p);
                if (control != null)
                {
                    control.DataContext = p;
                    AddToPropTreeViewParent = false;
                    treeView.Items.Add(control);
                    continue;
                }

                
                if (CreateEditorByList(p, propTreeViewParent, treeView))
                    continue;

                // if unable to use prebuilt controls, use old system
                switch (p.PropertyType)
                {
                    case "FrostySdk.Ebx.PointerRef":
                        CreatePointerRefControl(p, ref propTreeViewParent);
                        break;
                    // Unknown/Other Struct
                    default:
                        var structProperties = ModdableProperty.GetModdableProperties(p.PropertyValue, (s, n) =>
                        {
                            _ = SaveToRootObject();
                        }).ToList();
                        propTreeViewParent.Header = p.PropertyName;
                        foreach (var property in structProperties)
                        {
                            _ = CreateEditor(property, propTreeViewParent);
                        }
                        //treeView.Items.Add(propTreeViewParent);
                        break;


                }

                if (AddToPropTreeViewParent)
                    treeView.Items.Add(propTreeViewParent);

            }
            return success;
        }


        public bool CreatePointerRefControl(ModdableProperty p, ref TreeViewItem propTreeViewParent)
        {

            if (Utilities.HasProperty(p.PropertyValue, "Internal"))
            {
                var Internal = p.PropertyValue.GetPropertyValue("Internal");
                if (Internal != null && Utilities.HasProperty(Internal, "Points"))
                {
                    //var props = Utilities.GetProperties(Internal);
                    var interalsMP = ModdableProperty.GetModdableProperties(Internal, Modprop_PropertyChanged).ToList();

                    //propTreeViewParent.Header = p.PropertyName;
                    foreach (var property in interalsMP)
                    {
                        _ = CreateEditor(property, propTreeViewParent);
                    }

                    //// Must be Float Curve if it has Points
                    //var FloatCurve = Internal;
                    //               // Guid
                    //               var spGuid = new StackPanel() { Orientation = Orientation.Horizontal };
                    //               var lblGuid = new Label() { Content = "__Guid" };
                    //               spGuid.Children.Add(lblGuid);
                    //               var txtGuid = new Label() { Name = p.PropertyName + "___Guid", Content = FloatCurve.GetPropertyValue("__InstanceGuid").ToString() };
                    //               spGuid.Children.Add(txtGuid);
                    //               propTreeViewParent.Items.Add(spGuid);


                    //               // Min X
                    //               var spMinX = new StackPanel() { Orientation = Orientation.Horizontal };
                    //               var lblMinX = new Label() { Content = "MinX" };
                    //               spMinX.Children.Add(lblMinX);
                    //               var txtMinX = new TextBox() { Name = "MinX", Text = FloatCurve.GetPropertyValue("MinX").ToString() };

                    //               spMinX.Children.Add(txtMinX);
                    //               propTreeViewParent.Items.Add(spMinX);

                    //               // Max X 
                    //               var spMaxX = new StackPanel() { Orientation = Orientation.Horizontal };
                    //               var lblMaxX = new Label() { Content = "MaxX" };
                    //               spMaxX.Children.Add(lblMaxX);
                    //               var txtMaxX = new TextBox() { Name = "MaxX", Text = FloatCurve.GetPropertyValue("MaxX").ToString() };
                    //               spMaxX.Children.Add(txtMaxX);
                    //               propTreeViewParent.Items.Add(spMaxX);

                    //               TreeViewItem PointsTreeViewParent = new TreeViewItem();
                    //               PointsTreeViewParent.Name = "Points";
                    //               PointsTreeViewParent.Header = "Points";
                    //               propTreeViewParent.Items.Add(PointsTreeViewParent);


                    //               var numberOfPoints = (int)FloatCurve.GetPropertyValue("Points").GetPropertyValue("Count");
                    //               // Number of Points
                    //               var txtNumberOfPoints = new TextBox() { Name = p.PropertyName + "_NumberOfPoints", Text = numberOfPoints.ToString() };
                    //               txtNumberOfPoints.TextChanged += (object sender, TextChangedEventArgs e) =>
                    //               {
                    //                   AssetHasChanged(sender as TextBox, p.PropertyName);
                    //               };

                    //               Grid gridNumberOfPoints = new Grid();
                    //               gridNumberOfPoints.ColumnDefinitions.Add(new ColumnDefinition());
                    //               gridNumberOfPoints.ColumnDefinitions.Add(new ColumnDefinition());
                    //               Label lblNumberOfPoints = new Label() { Content = "Point Count: " };
                    //               Grid.SetColumn(lblNumberOfPoints, 0);
                    //               Grid.SetColumn(txtNumberOfPoints, 1);
                    //               gridNumberOfPoints.Children.Add(lblNumberOfPoints);
                    //               gridNumberOfPoints.Children.Add(txtNumberOfPoints);
                    //               PointsTreeViewParent.Items.Add(gridNumberOfPoints);

                    //               for (var i = 0; i < numberOfPoints; i++)
                    //               {
                    //                   var point = ((IList)FloatCurve.GetPropertyValue("Points"))[i];
                    //                   if (point != null)
                    //                   {
                    //                       TreeViewItem Child1Item = new TreeViewItem();
                    //                       Child1Item.Header = "[" + i.ToString() + "]";
                    //                       Child1Item.IsExpanded = true;

                    //                       var txtPointX = new TextBox()
                    //                       {
                    //                           Name = p.PropertyName + "_Points_" + i.ToString() + "_X"
                    //                           ,
                    //                           Text = point.GetPropertyValue("X").ToString()
                    //                       };
                    //                       txtPointX.LostFocus += (object sender, RoutedEventArgs e) =>
                    //                       {
                    //                           AssetHasChanged(sender as TextBox, p.PropertyName);
                    //                       };
                    //                       Child1Item.Items.Add(txtPointX);

                    //                       var txtPointY = new TextBox() { Name = p.PropertyName + "_Points_" + i.ToString() + "_Y", Text = point.GetPropertyValue("Y").ToString() };
                    //                       //txtPointY.PreviewLostKeyboardFocus += (object sender, KeyboardFocusChangedEventArgs e) =>
                    //                       //{
                    //                       txtPointY.LostFocus += (object sender, RoutedEventArgs e) =>
                    //                       {
                    //                           AssetHasChanged(sender as TextBox, p.PropertyName);
                    //                       };
                    //                       Child1Item.Items.Add(txtPointY);

                    //                       PointsTreeViewParent.Items.Add(Child1Item);
                    //                   }
                    //               }
                }

                // This is external?
                else
                {
                    TextBox externalTextbox = new TextBox();
                    externalTextbox.Text = "External PointerRef is not Supported";
                    externalTextbox.IsEnabled = false;
                    propTreeViewParent.Items.Add(externalTextbox);
                }
            }

            return true;
        }

        public async Task SaveToRootObject(bool forceReload = false)
        {
            await AssetManager.Instance.ModifyEbxAsync(AssetEntry.Name, Asset);
            //await Task.Run(() =>
            //{
            //    try
            //    {
            //        AssetManager.Instance.ModifyEbx(AssetEntry.Name, Asset);
            //    }
            //    catch (Exception)
            //    {
            //        //AssetManager.Instance.LogError($"Unable to modify EBX {AssetEntry.Name}");
            //        FileLogger.WriteLine($"Unable to modify EBX {AssetEntry.Name}");
            //    }
            //    //FrostyProject.Save("GameplayProject.fbproject", true);
            //});

            ////await loadingDialog.UpdateAsync("Saving hotspot", "Updating browsers");

            ////await Dispatcher.InvokeAsync(() =>
            ////{
            ////    if (EditorWindow != null)
            ////        EditorWindow.UpdateAllBrowsers();
            ////});

            await Dispatcher.InvokeAsync(() =>
            {
                if (EditorWindow != null)
                    EditorWindow.Log($"[{DateTime.Now.ToString("t")}] {asset.RootObject} Saved");

            });

            if (forceReload)
            {
                await LoadEbx(AssetEntry, Asset, EditorWindow);
            }
            //await CreateEditor(RootObjectProperties.OrderBy(x => x.PropertyName), TreeView1);

        }


        private void chkImportFromFiles_Checked(object sender, RoutedEventArgs e)
        {
            var listoffiles = FrostbiteModWriter.EbxResource.ListOfEBXRawFilesToUse;

            if (((CheckBox)sender).IsChecked.Value)
            {
                if (!listoffiles.Contains(AssetEntry.Filename))
                    listoffiles.Add(AssetEntry.Filename);
            }
            else
            {
                if (listoffiles.Contains(AssetEntry.Filename))
                    listoffiles.RemoveAll(x => x.ToLower() == AssetEntry.Filename.ToLower());
            }

            FrostbiteModWriter.EbxResource.ListOfEBXRawFilesToUse = listoffiles;
        }
    }


}
