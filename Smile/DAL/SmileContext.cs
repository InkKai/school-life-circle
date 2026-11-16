using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Smile.Models;

namespace Smile.DAL
{
    public class SmileContext : DbContext
    {
        public SmileContext() : base("name=SmileContext") { }

        public DbSet<User> Users { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<StoreCategory> StoreCategories { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }
        public DbSet<Redemption> Redemptions { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<StampCard> StampCards { get; set; }
        public DbSet<StampRecord> StampRecords { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 多條關聯都指向同一張表時，SQL Server 不允許多重串聯刪除，統一關閉
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Store>().HasRequired(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId);
            modelBuilder.Entity<Redemption>().HasRequired(r => r.RedeemedBy).WithMany().HasForeignKey(r => r.RedeemedById);
            modelBuilder.Entity<Redemption>().HasRequired(r => r.Store).WithMany().HasForeignKey(r => r.StoreId);
            modelBuilder.Entity<StampRecord>().HasRequired(r => r.Operator).WithMany().HasForeignKey(r => r.OperatorId);
            modelBuilder.Entity<StampRecord>().HasRequired(r => r.StampCard).WithMany(c => c.Records).HasForeignKey(r => r.StampCardId);
            modelBuilder.Entity<Friendship>().HasRequired(f => f.User).WithMany().HasForeignKey(f => f.UserId);
            modelBuilder.Entity<Friendship>().HasRequired(f => f.Friend).WithMany().HasForeignKey(f => f.FriendId);
            modelBuilder.Entity<UserCoupon>().HasOptional(u => u.GiftedFrom).WithMany().HasForeignKey(u => u.GiftedFromId);
            modelBuilder.Entity<User>().HasOptional(u => u.ReferredBy).WithMany().HasForeignKey(u => u.ReferredById);
            modelBuilder.Entity<Coupon>().HasOptional(c => c.Campaign).WithMany(c => c.Coupons).HasForeignKey(c => c.CampaignId);
            base.OnModelCreating(modelBuilder);
        }
    }
}
