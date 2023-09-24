using FMT.Controls.Models;
using FMT.FileTools;
using FMT.Logging;
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
    public partial class AssetEntryViewer : UserControl
    {

        public static readonly DependencyProperty AssetEntryProperty;

        public static readonly DependencyProperty AssetModifiedProperty;

        protected IEditorWindow EditorWindow;

        protected List<object> objects;

        protected List<object> rootObjects;

        protected Dictionary<Guid, EbxAsset> dependentObjects = new Dictionary<Guid, EbxAsset>();

        private List<ModdableEntity> _rootObjProps;

        private AssetEntry AssetEntry { get; set; }

        public List<ModdableEntity> RootObjectProperties
        {
            get
            {
                if (_rootObjProps == null)
                {
                    _rootObjProps = new List<ModdableEntity>();
                    if (AssetEntry != null)
                    {
                        var fields = ModdableField.GetModdableFields(AssetEntry).ToList();
                        var props = ModdableProperty.GetModdableProperties(AssetEntry).ToList();
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
            }
        }

        [Obsolete("Incorrect usage of Editor Windows")]

        public AssetEntryViewer()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        //[Deprecated("This is only used for Testing Purposes", DeprecationType.Deprecate, 1)]
        public AssetEntryViewer(AssetEntry assetEntry)
        { 
            InitializeComponent();
            // intialise objs
            this.DataContext = this;
            this.TreeView1.DataContext = assetEntry;
        }

        public static Editor CurrentEditorInstance { get; set; }

        public async Task<bool> LoadEntry(
            AssetEntry inAssetEntry
            , IEditorWindow inEditorWindow)
        {

            if (inAssetEntry == null)
                return false;

            AssetEntry = inAssetEntry;

            _rootObjProps = null;
            EditorWindow = inEditorWindow;

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
                }).ToList();
                propTreeViewParent.Header = p.PropertyName;
                propTreeViewParent.ToolTip = p.GetPropertyDescription();
                foreach (var property in structProperties)
                {
                    _ = CreateEditor(property, propTreeViewParent);
                }
                var structFields = ModdableField.GetModdableFields(p.PropertyValue, (s, n) =>
                {
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

 
    }


}
