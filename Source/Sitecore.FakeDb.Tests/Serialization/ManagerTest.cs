namespace Sitecore.FakeDb.Tests.Serialization
{
  using FluentAssertions;
  using Sitecore.Data.Serialization;
  using Sitecore.IO;
  using System;
  using Xunit;

  public class ManagerTest
  {
    [Fact]
    public void ShouldDumpItem()
    {
      // arrange
      using (var db = new Db { new DbItem("home") })
      {
        var item = db.GetItem("/sitecore/content/home");

        // act
        Assert.DoesNotThrow(() => Manager.DumpItem(item));
      }
    }

    [Fact]
    public void ShouldDumpAndLoadItem()
    {
      // arrange
      using (var db = new Db
                        {
                          new DbItem("home") { { "Title", "Welcome!" } }
                        })
      {
        var item = db.GetItem("/sitecore/content/home");
        var itemReference = new ItemReference(item);
        var path = PathUtils.GetFilePath(itemReference.ToString());

        Manager.DumpItem(path, item);

        item.Delete();

        // act
        Manager.LoadItem(path, new LoadOptions(db.Database));

        // assert
        db.GetItem("/sitecore/content/home")["Title"].Should().Be("Welcome!");
      }
    }

    [Fact]
    public void ShouldDumpAndLoadDatabase()
    {
      // arrange
      using (var db = new Db
                        {
                          new DbItem("home") { { "Title", "Welcome!" } }
                        })
      {
        var item = db.GetItem("/sitecore/content/home");
        var path = PathUtils.GetDatabaseRoot("master");

        Manager.DumpTree(db.Database.GetItem("/sitecore"));

        item.Delete();

        // act
        Manager.LoadTree(path, new LoadOptions(db.Database));

        // assert
        db.GetItem("/sitecore/content/home")["Title"].Should().Be("Welcome!");
      }
    }
  }
}
