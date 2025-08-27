using System.Linq;
using System.Threading.Tasks;
using App.Domain.Entities;

namespace App.Infrastructure.Data;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!db.Set<Role>().Any())
        {
            db.Set<Role>().AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Officer" },
                new Role { Name = "Reviewer" }
            );
        }

        if (!db.Set<AssetType>().Any())
        {
            var realEstate = new AssetType { Name = "RealEstate" };
            db.Set<AssetType>().Add(realEstate);

            db.Set<FieldDefinition>().AddRange(
                new FieldDefinition { AssetType = realEstate, Name = "OwnershipDocumentNumber", DataType = DataType.Text, PrivacyLevel = PrivacyLevel.Restricted, Required = true },
                new FieldDefinition { AssetType = realEstate, Name = "TotalAreaM2", DataType = DataType.Number, PrivacyLevel = PrivacyLevel.Public, Required = true }
            );
        }

        await db.SaveChangesAsync();
    }
}
