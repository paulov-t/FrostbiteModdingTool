using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using MahApps.Metro.Controls;
using SdkGenerator;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using v2k4FIFAModdingCL;

namespace FIFAModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for BuildSDKAndCache.xaml
    /// </summary>
    public partial class BuildSDKAndCache : MetroWindow
    {
        public BuildSDKAndCache(Window owner)
        {
            InitializeComponent();
            this.DataContext = this;
            Owner = owner;
        }
    }
}
