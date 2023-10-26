using FMT.Controls.Models;
using FMT.FileTools;
using FMT.Logging;
using FMT.Models;
using FMT.Pages.Common;
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
    public partial class Editor : UserControl
    {

        protected IEditorWindow EditorWindow;

        protected List<object> objects;

        protected List<object> rootObjects;

        protected Dictionary<Guid, EbxAsset> dependentObjects = new Dictionary<Guid, EbxAsset>();

        //protected EbxAsset asset;

        public object RootObject { get { return Asset.RootObject; } }

        private List<ModdableEntity> _rootObjProps;

        public List<ModdableEntity> RootObjectProperties
        {
            get
            {
                if (_rootObjProps == null)
                {
                    _rootObjProps = new List<ModdableEntity>();
                    if (RootObject != null)
                    {
                        var vanillaRootObject = AssetManager.Instance.GetEbx((EbxAssetEntry)AssetEntry, false).RootObject;

                        var fields = ModdableField.GetModdableFields(RootObject, Modprop_PropertyChanged, vanillaRootObject).ToList();
                        var props = ModdableProperty.GetModdableProperties(RootObject, Modprop_PropertyChanged, vanillaRootObject).ToList();
                        _rootObjProps.AddRange(fields);
                        _rootObjProps.AddRange(props);
                        _rootObjProps = _rootObjProps
                            .OrderByDescending(x => x.PropertyName == "BaseField")
                            .ThenByDescending(x => x.PropertyName == "Name")
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

                AssetManager.Instance.ModifyEbx(AssetEntry.Name, Asset);
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
                EditorWindow.Log($"[{DateTime.Now.ToString("t")}] {Asset.RootObject} Saved");
        }

        public void RevertAsset()
        {
            AssetManager.Instance.RevertAsset(AssetEntry);
            this.Visibility = Visibility.Collapsed;
        }

        public IEnumerable<object> RootObjects => Asset.RootObjects;

        public IEnumerable<object> Objects => Asset.Objects;

        //public IAssetEntry AssetEntry { get; set; }

        public static readonly DependencyProperty AssetEntryProperty = DependencyProperty.Register("AssetEntry", typeof(IAssetEntry), typeof(Editor), new FrameworkPropertyMetadata(null));
        public IAssetEntry AssetEntry
        {
            get => (IAssetEntry)GetValue(AssetEntryProperty);
            set => SetValue(AssetEntryProperty, value);
        }


        //public EbxAsset Asset { get { return asset; } set { asset = value; if(PropertyChanged != null) PropertyChanged.Invoke(this, null); } }
        //public EbxAsset Asset { get { return asset; } set { asset = value; } }

        public static readonly DependencyProperty AssetProperty = DependencyProperty.Register("Asset", typeof(EbxAsset), typeof(Editor), new FrameworkPropertyMetadata(null));
        public EbxAsset Asset
        {
            get => (EbxAsset)GetValue(AssetProperty);
            set => SetValue(AssetProperty, value);
        }

        //public static readonly DependencyProperty AssetPathProperty = DependencyProperty.Register("AssetPath", typeof(AssetPath), typeof(Editor), new FrameworkPropertyMetadata(null));
        //public AssetPath AssetPath
        //{
        //    get => (AssetPath)GetValue(AssetPathProperty);
        //    set => SetValue(AssetPathProperty, value);
        //}

        public AssetPath TheAssetPath { get; set; }

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
            , IEditorWindow inEditorWindow
            , AssetPath assetPath)
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
            AssetEntry = inAssetEntry;
            Asset = inAsset;
            EditorWindow = inEditorWindow;
            TheAssetPath = assetPath;

            bool success = true;

            _ = RootObjectProperties;
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

        public Control GetMatchingTypedControl(ModdableEntity d)
        {
            var propertyNSearch = d.PropertyType.ToLower().Replace(".", "_", StringComparison.OrdinalIgnoreCase);


            if (UsableTypes == null || UsableTypes.Count == 0)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                //UsableTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
                foreach (Assembly assembly in assemblies
                    .Where(x => !x.FullName.Contains("EbxClasses", StringComparison.OrdinalIgnoreCase))
                    .Where(x => x.GetExportedTypes().Any(y=>y.BaseType == typeof(UserControl)))
                    )
                {
                    foreach (Type t in assembly.GetExportedTypes()
                        .Where(y => !Guid.TryParse(y.Name, out _))
                        .Where(y => y.BaseType == typeof(UserControl)))
                    {
                        UsableTypes.Add(t);
                    }
                }
            }

            var ty = UsableTypes.FirstOrDefault(x => x.Name.Equals(propertyNSearch, StringComparison.OrdinalIgnoreCase));

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

        //public Control GetMatchingTypedControl(ModdableProperty moddableProperty)
        //{
        //    var propertyNSearch = moddableProperty.PropertyType.Replace(".", "_", StringComparison.OrdinalIgnoreCase);

        //    if (UsableTypes == null)
        //        UsableTypes = Assembly.GetExecutingAssembly().GetTypes();

        //    var ty = UsableTypes.FirstOrDefault(x => x.Name.Contains(propertyNSearch, StringComparison.OrdinalIgnoreCase));

        //    if (ty != null)
        //    {
        //        Control control = Activator.CreateInstance(ty) as Control;
        //        if (control != null)
        //        {
        //            return control;
        //        }
        //    }

        //    return null;
        //}


        public static List<Type> UsableTypes { get; private set; } = new List<Type>();

        public async Task<bool> CreateEditor(ModdableEntity d, TreeView treeView)
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



        public bool CreateEditor(
            ModdableEntity d
            , TreeViewItem treeViewItem
            , TreeView treeView = null
            , bool onlyTypedControl = false
            )
        {
            
            Control control = GetMatchingTypedControl(d);
            if (control != null)
            {
                control.ToolTip = d.PropertyDescription;
                control.DataContext = d;
                treeViewItem.Items.Add(control);
                return true;
            }

            if (onlyTypedControl)
                return true;

            if (CreateEditorByList(d, treeViewItem, treeView))
                return true;

            // Properties
            var structProperties = ModdableProperty.GetModdableProperties(d.PropertyValue, (s, n) =>
            {
                _ = SaveToRootObject();
            }).ToList();
            TreeViewItem structTreeView = new TreeViewItem();
            structTreeView.ToolTip = d.PropertyDescription;
            structTreeView.Header = d.PropertyName;
            foreach (var property in structProperties)
            {
                if (!CreateEditor(property, structTreeView))
                    return false;
            }
            // Fields
            var structFields = ModdableField.GetModdableFields(d.PropertyValue, (s, n) =>
            {
                _ = SaveToRootObject();
            }).ToList();
            foreach (var property in structFields)
            {
                if (!CreateEditor(property, structTreeView, null, true))
                    return false;
            }
            treeViewItem.ToolTip = d.GetPropertyDescription();
            treeViewItem.Items.Add(structTreeView);
            return true;
           
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

        public bool CreateEditorByList(ModdableEntity p, TreeViewItem propTreeViewParent, TreeView treeView = null)
        {
            // TODO:! Moddable Field list not supported
            if (p is ModdableField)
                return false;

            // Is a list / array property
            if (p.PropertyType.Contains("List`1"))
            {
                var genArgsType = ((ModdableProperty)p).Property.PropertyType.GetGenericArguments()[0];
                // get count in array
                var countOfArray = (int)p.PropertyValue.GetPropertyValue("Count");
                if (countOfArray == 0)
                {
                    //propTreeViewParent.Items.Add(new TreeViewItem() { Header = "No items to display" });
                }
                else
                {
                    propTreeViewParent.Header += $" [{genArgsType}][{countOfArray}]";
                    // add buttons for array...
                    // get items in array
                    for (var i = 0; i < countOfArray; i++)
                    {
                        var itemOfArray = ((IList)p.PropertyValue)[i];
                        CreateEditor(new ModdableProperty(rootObject: p.RootObject, property: ((ModdableProperty)p).Property, arrayIndex: i
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
                }
                if (treeView != null)
                    treeView.Items.Add(propTreeViewParent);

                return true;
            }

            return false;
        }

        public async Task<bool> CreateEditor(List<ModdableEntity> moddableProperties, TreeView treeView)
        {
            bool success = true;

            treeView.Items.Clear();
            foreach (var p in moddableProperties)
            {
                TreeViewItem propTreeViewParent = new TreeViewItem();

                propTreeViewParent.Header = p.PropertyName;
                propTreeViewParent.ToolTip = p.GetPropertyDescription();

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
                var structProperties = ModdableProperty.GetModdableProperties(p.PropertyValue, (s, n) =>
                {
                    _ = SaveToRootObject();
                }).ToList();
                propTreeViewParent.Header = p.PropertyName;
                propTreeViewParent.ToolTip = p.GetPropertyDescription();
                foreach (var property in structProperties)
                {
                    _ = CreateEditor(property, propTreeViewParent);
                }
                var structFields = ModdableField.GetModdableFields(p.PropertyValue, (s, n) =>
                {
                    _ = SaveToRootObject();
                }).ToList();
                foreach (var property in structFields)
                {
                    if (!CreateEditor(property, propTreeViewParent, null, true))
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
                    var interalsMP = ModdableProperty.GetModdableProperties(Internal, Modprop_PropertyChanged).ToList();

                    foreach (var property in interalsMP)
                    {
                        _ = CreateEditor(property, propTreeViewParent);
                    }
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

            await Dispatcher.InvokeAsync(() =>
            {
                if (EditorWindow != null)
                    EditorWindow.Log($"[{DateTime.Now.ToString("t")}] {Asset.RootObject} Saved");

            });

            if (forceReload)
            {
                await LoadEbx(AssetEntry, Asset, EditorWindow, TheAssetPath);
            }

            TheAssetPath.AssetChanged();

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
