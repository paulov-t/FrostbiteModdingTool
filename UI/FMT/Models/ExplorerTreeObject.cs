using FrostySdk.Managers;
using System.Collections.Generic;
using System.Linq;

namespace FIFAModdingUI.Models
{


    //public class ExplorerTreeObject
    //{
    //	public ExplorerTreeObject(string path)
    //	{
    //		Title = path;
    //	}
    //	public ExplorerTreeObject(string path, EbxAssetEntry assetEntry)
    //	{
    //		Title = path;
    //		AssociatedEntry = assetEntry;

    //		//Children = children;
    //	}


    //	public ExplorerTreeObject(List<EbxAssetEntry> assetEntries)
    //	{
    //		Tiers = new ObservableCollection<ExplorerTreeObject>();
    //		foreach (EbxAssetEntry assetEntry in assetEntries)
    //		{
    //			var pathSplit = assetEntry.Path.Split('/');
    //			var firstIndexOfTopLevelPath = assetEntry.Path.IndexOf('/');

    //			var title = assetEntry.Path.Substring(0, firstIndexOfTopLevelPath);

    //			var index = 0;
    //			foreach(var p in pathSplit)
    //               {
    //				var t = new ExplorerTreeObject(p);
    //                   if (Tiers.Contains(t))
    //                   {
    //					foreach(var subTier in Tiers.T)
    //                       {

    //                       }
    //                   }
    //				index++;
    //               }

    //		}


    //	}

    //	public ObservableCollection<ExplorerTreeObject> Tiers;

    //	public string Title { get; set; }

    //	public AssetEntry AssociatedEntry { get; set; }

    //	public ObservableCollection<ExplorerTreeObject> Children { get; set; }

    //       public override string ToString()
    //       {
    //           return base.ToString();
    //       }

    //     public override bool Equals(object obj)
    //     {
    //ExplorerTreeObject other = obj as ExplorerTreeObject;
    //if(other != null)
    //         {
    //	return other.Title.ToLower() == this.Title.ToLower();
    //         }
    //         return base.Equals(obj);

    //     }

    //     public override int GetHashCode()
    //     {
    //         return base.GetHashCode();
    //     }
    // }

    public class Item
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class FileItem : Item
    {

    }

    public class DirectoryItem : Item
    {
        public List<Item> Items { get; set; }

        public DirectoryItem()
        {
            Items = new List<Item>();
        }
    }

    public class ItemProvider
    {
        public List<Item> GetItems(List<EbxAssetEntry> assets)
        {
            var items = new List<Item>();
            if (assets.Count() == 0)
                return items;

            foreach (var directory in assets)
            {
                var directoryName = directory.Path.Split('/')[0];
                var directoryPathSplit = directory.Path.Split('/');
                var directoryPath = directory.Path;
                if (directoryPathSplit.Length > 0)
                {
                    directoryPath = directoryPathSplit[1];
                }

                var item = new DirectoryItem
                {
                    Name = directoryName,
                    Path = directoryPath,
                    Items = GetItems(assets.Where(x => x.Path.EndsWith(directoryPath)).ToList())
                };

                items.Add(item);
            }

            foreach (var file in assets)
            {
                var item = new FileItem
                {
                    Name = file.Name,
                    Path = file.Path
                };

                items.Add(item);
            }

            return items;
        }
    }
}
