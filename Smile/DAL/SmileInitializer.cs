using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using Smile.Infrastructure;
using Smile.Models;

namespace Smile.DAL
{
    /// <summary>資料庫不存在時建立，並放入示範資料。示範帳號見專案根目錄的 README_學區生活圈.md。</summary>
    public class SmileInitializer : CreateDatabaseIfNotExists<SmileContext>
    {
        /// <summary>
        /// Azure SQL 通常是先在入口網站建立「空的」資料庫，這時資料庫已存在但沒有資料表，
        /// CreateDatabaseIfNotExists 會略過不建表。這裡補上：資料庫存在但沒有資料表時，自己建表並放入示範資料。
        /// </summary>
        public override void InitializeDatabase(SmileContext context)
        {
            if (context.Database.Exists() &&
                context.Database.SqlQuery<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'").Single() == 0)
            {
                // 用 EF 的移轉引擎建表（CreateDatabaseScript 不會建立 [Index] 唯一索引，所以不用它）
                new System.Data.Entity.Migrations.DbMigrator(new EmptyDatabaseConfiguration()).Update();
                Seed(context);
                context.SaveChanges();
                return;
            }
            base.InitializeDatabase(context);
        }

        /// <summary>只在「資料庫存在但是空的」時使用：自動移轉，依目前的 Model 建立所有資料表與索引</summary>
        private sealed class EmptyDatabaseConfiguration : System.Data.Entity.Migrations.DbMigrationsConfiguration<SmileContext>
        {
            public EmptyDatabaseConfiguration()
            {
                AutomaticMigrationsEnabled = true;
                ContextKey = "Smile.DAL.SmileContext";
            }
        }

        /// <summary>示範帳號密碼：優先讀 Web.config／Azure 應用程式設定，沒有才用 README 裡的本機預設值</summary>
        private static string DemoPassword(string key, string fallback)
        {
            var v = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        protected override void Seed(SmileContext db)
        {
            var now = DateTime.Now;

            // 座標為概略位置（依 OpenStreetMap 地圖目視標定）
            var schools = new List<School>
            {
                new School { Name = "國立臺中科技大學", ShortName = "臺中科大", Latitude = 24.1505, Longitude = 120.6829 },
                new School { Name = "臺中市立臺中第一高級中等學校", ShortName = "臺中一中", Latitude = 24.1509, Longitude = 120.6868 },
                new School { Name = "國立臺灣體育運動大學", ShortName = "臺體大", Latitude = 24.1512, Longitude = 120.6897 },
            };
            db.Schools.AddRange(schools);

            var catDefs = new[] {
                new[] { "餐飲", "🍱", "#F97316" }, new[] { "飲料甜點", "🥤", "#D97706" }, new[] { "服飾", "👕", "#2563EB" },
                new[] { "鞋子", "👟", "#0D9488" }, new[] { "飾品", "💍", "#DB2777" }, new[] { "藥妝", "💊", "#16A34A" }, new[] { "眼鏡", "👓", "#7C3AED" }
            };
            var cats = catDefs.Select((c, i) => new StoreCategory { Name = c[0], Icon = c[1], Color = c[2], SortOrder = i }).ToList();
            db.StoreCategories.AddRange(cats);
            db.SaveChanges();

            Func<string, string, string, string, int?, User> user = (email, name, role, pwd, school) =>
            {
                string hash, salt;
                Security.HashPassword(pwd, out hash, out salt);
                return new User { Email = email, DisplayName = name, Role = role, PasswordHash = hash, PasswordSalt = salt, SchoolId = school, MemberCode = Security.NewMemberCode(), InviteCode = Security.NewInviteCode(), CreatedAt = now };
            };
            var admin = user("admin@smile.test", "平台管理員", AppRoles.Admin, DemoPassword("DemoPassword:Admin", "Smile!Admin2026"), null);
            var merchant = user("merchant@smile.test", "示範店家老闆", AppRoles.Merchant, DemoPassword("DemoPassword:Merchant", "Smile!Shop2026"), null);
            var member = user("member@smile.test", "示範同學", AppRoles.Member, DemoPassword("DemoPassword:Member", "Smile!Member2026"), schools[0].Id);
            var friend = user("friend@smile.test", "示範好友阿明", AppRoles.Member, DemoPassword("DemoPassword:Friend", "Smile!Friend2026"), schools[1].Id);
            db.Users.AddRange(new[] { admin, merchant, member, friend });
            db.SaveChanges();

            // 示範會員：只用來讓「大家都在領」、評論、報表有內容。密碼是隨機產生的，沒有人能登入。
            var demoNames = new[] { "小安", "阿哲", "Mia", "秀英阿嬤", "Kevin", "小涵", "阿翰", "Yuki" };
            var demos = demoNames.Select((n, i) =>
            {
                var d = user("demo" + (i + 1) + "@smile.test", n + "（示範）", AppRoles.Member, Guid.NewGuid().ToString("N") + "aA1!", schools[i % schools.Count].Id);
                d.CreatedAt = now.AddDays(-(i * 3 + 1));
                return d;
            }).ToList();
            demos[5].ReferredById = member.Id;   // 小涵是示範同學邀請來的
            db.Users.AddRange(demos);
            db.SaveChanges();

            Func<string, int, string, double, double, string, bool, bool, bool, Store> store = (name, cat, addr, lat, lng, pay, stepFree, ramp, toilet) => new Store
            {
                Name = name, CategoryId = cats[cat].Id, Address = addr, Latitude = lat, Longitude = lng, PaymentMethods = pay,
                StepFreeEntrance = stepFree, HasRamp = ramp, AccessibleToilet = toilet, AccessibilityVerified = false,
                IsDemo = true, DataSource = DataSources.Demo, AccessibilityKnown = true,
                Status = StoreStatus.Approved, OwnerId = merchant.Id, CreatedAt = now,
                OpenHours = "11:00–21:00", Phone = "04-0000-0000",
                Description = "這是虛構的示範店家，用來展示領券與核銷功能，不代表真實商家。"
            };
            var stores = new List<Store>
            {
                store("學區便當屋（示範）", 0, "臺中市北區（示範地址）", 24.1489, 120.6851, "現金、LINE Pay", true, false, false),
                store("一中麵食館（示範）", 0, "臺中市北區（示範地址）", 24.1462, 120.6849, "現金", true, true, true),
                store("操場邊早午餐（示範）", 0, "臺中市北區（示範地址）", 24.1478, 120.6921, "現金、台灣Pay", false, false, false),
                store("放學手搖飲（示範）", 1, "臺中市北區（示範地址）", 24.1468, 120.6874, "現金、LINE Pay、台灣Pay", true, false, false),
                store("三民路雪花冰（示範）", 1, "臺中市北區（示範地址）", 24.1505, 120.6826, "現金", true, true, false),
                store("學區古著（示範）", 2, "臺中市北區（示範地址）", 24.1458, 120.6868, "現金、LINE Pay", false, false, false),
                store("一中街潮流服飾（示範）", 2, "臺中市北區（示範地址）", 24.1453, 120.6857, "現金、台灣Pay", true, false, false),
                store("快跑鞋舖（示範）", 3, "臺中市北區（示範地址）", 24.1450, 120.6880, "現金、LINE Pay", true, true, false),
                store("小日子飾品（示範）", 4, "臺中市北區（示範地址）", 24.1461, 120.6838, "現金", false, false, false),
                store("健康藥妝（示範）", 5, "臺中市北區（示範地址）", 24.1482, 120.6892, "現金、LINE Pay、台灣Pay", true, true, true),
                store("清晰眼鏡行（示範）", 6, "臺中市北區（示範地址）", 24.1493, 120.6870, "現金、台灣Pay", true, true, false),
            };
            // 示範店家的示意照片（Unsplash 免費授權，見 Content/img/photos/CREDITS.txt）
            var photos = new[] { "bento", "noodle", "brunch", "bubbletea", "shavedice", "vintage", "fashion", "shoes", "accessories", "drugstore", "glasses" };
            for (int i = 0; i < stores.Count && i < photos.Length; i++) stores[i].PhotoUrl = "~/Content/img/photos/" + photos[i] + ".jpg";
            // 老闆的話：讓店家頁有一點人情味（示範店家，內容是虛構的）
            var words = new[] {
                "便當每天現做，吃不飽再來加飯！", "湯頭熬八小時，同學都說像家裡的味道。", "考試週早點來，幫你留靠窗的位子。",
                "茶是每天早上現泡的，甜度冰塊都可以調。", "芒果是當季的，冬天改賣草莓喔。", "每件古著我都親手洗過、整理過。",
                "學生憑證件都有折扣，歡迎來逛。", "鞋子可以試穿到滿意為止。", "飾品很多是自己做的，也可以客製。",
                "不確定買哪一種，直接問藥師最快。", "配眼鏡不用急，慢慢試到看得清楚。"
            };
            for (int i = 0; i < stores.Count && i < words.Length; i++) stores[i].OwnerMessage = words[i];
            // 精選店家：首頁的曝光位置（加值方案的示範）
            foreach (var i in new[] { 0, 3, 4, 6, 10 }) stores[i].IsFeatured = true;
            var pending = store("新開的文具小舖（示範）", 4, "臺中市北區（示範地址）", 24.1471, 120.6830, "現金", true, false, false);
            pending.PhotoUrl = "~/Content/img/photos/stationery.jpg";
            pending.Status = StoreStatus.Pending;
            stores.Add(pending);
            db.Stores.AddRange(stores);
            db.SaveChanges();

            var menu = new Dictionary<int, string[]>
            {
                { 0, new[] { "招牌雞腿便當|95", "排骨便當|85", "素食便當|75" } },
                { 1, new[] { "牛肉麵|130", "榨菜肉絲麵|80", "燙青菜|35" } },
                { 2, new[] { "總匯三明治|65", "蛋餅|40", "鮮奶茶|45" } },
                { 3, new[] { "珍珠奶茶|55", "四季春|35", "檸檬綠|50" } },
                { 4, new[] { "芒果雪花冰|120", "花生豆花|50" } },
                { 5, new[] { "古著襯衫|450", "牛仔外套|890" } },
                { 6, new[] { "素色 T 恤|290", "棒球外套|990" } },
                { 7, new[] { "帆布鞋|1290", "運動襪三入|199" } },
                { 8, new[] { "耳環|190", "手機吊飾|150" } },
                { 9, new[] { "防曬乳|399", "護唇膏|129" } },
                { 10, new[] { "學生鏡框|1500", "免費驗光|0" } },
            };
            foreach (var kv in menu)
                foreach (var item in kv.Value)
                {
                    var p = item.Split('|');
                    db.Products.Add(new Product { StoreId = stores[kv.Key].Id, Name = p[0], Price = int.Parse(p[1]), IsAvailable = true });
                }

            Func<int, string, string, int, int, Coupon> coupon = (s, title, desc, days, qty) => new Coupon
            {
                StoreId = stores[s].Id, Title = title, Description = desc, StartDate = now.Date.AddDays(-7),
                EndDate = now.Date.AddDays(days), TotalQuantity = qty, IssuedCount = 0, IsActive = true
            };
            var coupons = new List<Coupon>
            {
                coupon(0, "便當折 10 元", "出示 QR Code 即可折抵，每人限用一次。", 30, 100),
                coupon(1, "牛肉麵加麵免費", "限內用。", 20, 50),
                coupon(2, "早餐飲料第二杯半價", "限上午 11 點前。", 3, 80),
                coupon(3, "手搖飲買一送一", "限中杯，平日下午 2–5 點。", 14, 60),
                coupon(4, "雪花冰折 20 元", "", 25, 40),
                coupon(5, "古著全面 9 折", "特價品除外。", 45, 30),
                coupon(7, "帆布鞋折 100 元", "", 30, 20),
                coupon(9, "藥妝滿 300 折 30", "處方藥除外。", 60, 100),
                coupon(10, "學生鏡框 85 折", "需出示學生證或優惠券。", 90, 50),
            };
            // 活動：配合學校行事曆的主題檔期（示範）。日期用「今天」推算，不管什麼時候建立資料庫都有一檔進行中。
            var exam = new Campaign { Title = "考試週應援", Tagline = "熬夜讀書辛苦了，吃飽喝足再上場", StartDate = now.Date.AddDays(-5), EndDate = now.Date.AddDays(9), Theme = "indigo", IsActive = true };
            var club = new Campaign { Title = "社團成果展週", Tagline = "看完表演，帶朋友一起逛商圈", StartDate = now.Date.AddDays(14), EndDate = now.Date.AddDays(21), Theme = "rose", IsActive = true };
            db.Campaigns.AddRange(new[] { exam, club });
            foreach (var i in new[] { 0, 1, 2, 3 }) coupons[i].Campaign = exam;
            db.Coupons.AddRange(coupons);
            db.SaveChanges();

            // 示範：同學已領一張、已用一張，讓報表有資料
            var uc1 = new UserCoupon { CouponId = coupons[0].Id, UserId = member.Id, Code = Security.NewCouponCode(), Status = CouponStatus.Available, ClaimedAt = now.AddDays(-1) };
            var uc2 = new UserCoupon { CouponId = coupons[3].Id, UserId = member.Id, Code = Security.NewCouponCode(), Status = CouponStatus.Used, ClaimedAt = now.AddDays(-3), UsedAt = now.AddDays(-2) };
            // 一張 3 天內到期的券，讓首頁的「快到期提醒」有東西可以看
            var uc3 = new UserCoupon { CouponId = coupons[2].Id, UserId = member.Id, Code = Security.NewCouponCode(), Status = CouponStatus.Available, ClaimedAt = now.AddHours(-5) };
            db.UserCoupons.AddRange(new[] { uc1, uc2, uc3 });
            coupons[2].IssuedCount = 1;
            coupons[0].IssuedCount = 1;
            coupons[3].IssuedCount = 1;
            db.SaveChanges();
            ImportOpenStreetMap(db, cats, admin.Id, now);

            db.Redemptions.Add(new Redemption { UserCouponId = uc2.Id, StoreId = stores[3].Id, RedeemedById = merchant.Id, RedeemedAt = now.AddDays(-2) });
            // 示範集點卡：便當屋集 10 點、手搖飲集 8 點；示範同學在手搖飲已有 5 點
            stores[0].StampGoal = 10; stores[0].StampReward = "招牌便當一個";
            stores[3].StampGoal = 8; stores[3].StampReward = "中杯飲料一杯";
            var card = new StampCard { UserId = member.Id, StoreId = stores[3].Id, Stamps = 5, RewardsRedeemed = 0, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-1) };
            db.StampCards.Add(card);
            db.SaveChanges();
            for (int i = 5; i >= 1; i--)
                db.StampRecords.Add(new StampRecord { StampCardId = card.Id, Kind = StampKinds.Stamp, Change = 1, OperatorId = merchant.Id, CreatedAt = now.AddDays(-2 * i) });
            db.Friendships.Add(new Friendship { UserId = member.Id, FriendId = friend.Id, CreatedAt = now.AddDays(-5) });
            db.Friendships.Add(new Friendship { UserId = friend.Id, FriendId = member.Id, CreatedAt = now.AddDays(-5) });
            db.Favorites.Add(new Favorite { UserId = member.Id, StoreId = stores[10].Id, CreatedAt = now });
            db.Reviews.Add(new Review { UserId = member.Id, StoreId = stores[0].Id, Rating = 5, Comment = "份量很多，老闆人很好。（示範評論）", CreatedAt = now.AddDays(-1) });
            db.Friendships.Add(new Friendship { UserId = member.Id, FriendId = demos[5].Id, CreatedAt = demos[5].CreatedAt });
            db.Friendships.Add(new Friendship { UserId = demos[5].Id, FriendId = member.Id, CreatedAt = demos[5].CreatedAt });
            db.SaveChanges();

            SeedDemoActivity(db, demos, coupons, stores, merchant, now);

            // 示範同學：前 5 天都有簽到（今天還沒簽，展示時可以現場抽）
            var demoPrizes = new[] { "xp10", "xp5", "xp20", "xp5", "xp10" };
            for (int i = 0; i < demoPrizes.Length; i++)
                db.CheckIns.Add(new CheckIn { UserId = member.Id, Day = now.Date.AddDays(-(i + 1)), Prize = demoPrizes[i], Xp = Gacha.Find(demoPrizes[i]).Xp, CreatedAt = now.Date.AddDays(-(i + 1)).AddHours(8) });
            db.SaveChanges();

            // 依目前的紀錄發給每個人已達成的徽章（之後新達成的才會跳出慶祝）
            foreach (var id in db.Users.Select(u => u.Id).ToList())
                Progress.Sync(db, id, Progress.GetStats(db, id));

            // 示範資料的徽章都是剛剛一起發的，把日期往前錯開，看起來才像陸續拿到的
            foreach (var g in db.UserBadges.ToList().GroupBy(b => b.UserId))
            {
                var list = g.OrderBy(b => b.Id).ToList();
                for (int i = 0; i < list.Count; i++) list[i].EarnedAt = now.AddDays(-2 * (list.Count - i)).AddHours(-3);
            }
            db.SaveChanges();
        }

        /// <summary>示範會員的活動紀錄：領券、到店使用、收藏、評論、集點。全部只發生在示範店家。</summary>
        private static void SeedDemoActivity(SmileContext db, List<User> demos, List<Coupon> coupons, List<Store> stores, User merchant, DateTime now)
        {
            db.Configuration.AutoDetectChangesEnabled = true;

            // { 會員, 優惠券, 幾天前領的, 是否已到店使用 }
            var acts = new[] {
                new[] { 0, 3, 1, 1 }, new[] { 0, 0, 2, 0 }, new[] { 1, 3, 1, 1 }, new[] { 1, 1, 3, 0 }, new[] { 2, 3, 2, 0 }, new[] { 2, 4, 1, 1 },
                new[] { 3, 8, 4, 1 }, new[] { 3, 7, 2, 0 }, new[] { 4, 0, 1, 1 }, new[] { 4, 3, 5, 0 }, new[] { 5, 1, 2, 1 }, new[] { 5, 4, 6, 0 },
                new[] { 6, 0, 3, 0 }, new[] { 6, 5, 2, 0 }, new[] { 7, 3, 1, 0 }, new[] { 7, 1, 1, 0 }, new[] { 1, 0, 6, 1 }, new[] { 2, 1, 4, 0 }
            };
            var claimed = new List<UserCoupon>();
            foreach (var a in acts)
            {
                var at = now.AddDays(-a[2]).AddHours(-a[0]);
                var used = a[3] == 1;
                claimed.Add(new UserCoupon
                {
                    CouponId = coupons[a[1]].Id, UserId = demos[a[0]].Id, Code = Security.NewCouponCode(),
                    Status = used ? CouponStatus.Used : CouponStatus.Available, ClaimedAt = at, UsedAt = used ? at.AddHours(20) : (DateTime?)null
                });
                coupons[a[1]].IssuedCount++;
            }
            db.UserCoupons.AddRange(claimed);
            db.SaveChanges();
            foreach (var uc in claimed.Where(u => u.Status == CouponStatus.Used))
                db.Redemptions.Add(new Redemption { UserCouponId = uc.Id, StoreId = coupons.First(c => c.Id == uc.CouponId).StoreId, RedeemedById = merchant.Id, RedeemedAt = uc.UsedAt.Value });

            // { 會員, 店家 }
            var favs = new[] { new[] { 0, 3 }, new[] { 0, 0 }, new[] { 1, 3 }, new[] { 2, 4 }, new[] { 2, 3 }, new[] { 3, 10 }, new[] { 4, 0 }, new[] { 5, 1 }, new[] { 6, 5 }, new[] { 7, 3 }, new[] { 7, 6 } };
            foreach (var f in favs)
                db.Favorites.Add(new Favorite { UserId = demos[f[0]].Id, StoreId = stores[f[1]].Id, CreatedAt = now.AddDays(-f[0] - 1) });

            // { 會員, 店家, 星數, 內容 }
            var revs = new[] {
                new object[] { 0, 3, 5, "珍奶的珍珠很 Q，下課必買。" }, new object[] { 1, 3, 4, "買一送一跟同學分剛好。" },
                new object[] { 2, 4, 5, "芒果給很多，兩個人吃一碗就飽。" }, new object[] { 3, 10, 5, "老闆很有耐心，慢慢幫我調到看得清楚。" },
                new object[] { 4, 0, 4, "便當菜色每天換，價格實在。" }, new object[] { 5, 1, 5, "湯頭很濃，加麵免費超划算。" },
                new object[] { 1, 0, 5, "折 10 元很有感，中午都吃這間。" }
            };
            foreach (var r in revs)
                db.Reviews.Add(new Review { UserId = demos[(int)r[0]].Id, StoreId = stores[(int)r[1]].Id, Rating = (int)r[2], Comment = r[3] + "（示範評論）", CreatedAt = now.AddDays(-(int)r[0] - 1) });

            // 兩位示範會員在手搖飲集點
            var cards = new[] {
                new StampCard { UserId = demos[0].Id, StoreId = stores[3].Id, Stamps = 3, CreatedAt = now.AddDays(-6), UpdatedAt = now.AddDays(-1) },
                new StampCard { UserId = demos[1].Id, StoreId = stores[3].Id, Stamps = 2, CreatedAt = now.AddDays(-4), UpdatedAt = now.AddDays(-1) }
            };
            db.StampCards.AddRange(cards);
            db.SaveChanges();
            foreach (var c in cards)
                for (int i = 0; i < c.Stamps; i++)
                    db.StampRecords.Add(new StampRecord { StampCardId = c.Id, Kind = StampKinds.Stamp, Change = 1, OperatorId = merchant.Id, CreatedAt = now.AddDays(-i - 1) });
            db.SaveChanges();
        }

        /// <summary>匯入 App_Data/osm_stores.json：三校周邊兩公里內 OpenStreetMap 上的真實店家（只作店家名錄，不掛優惠）</summary>
        private static void ImportOpenStreetMap(SmileContext db, List<StoreCategory> cats, int ownerId, DateTime now)
        {
            var path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/osm_stores.json");
            if (path == null || !System.IO.File.Exists(path)) return;
            var root = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8));
            var byName = cats.ToDictionary(c => c.Name, c => c.Id);
            db.Configuration.AutoDetectChangesEnabled = false;
            foreach (var s in root["stores"])
            {
                var wc = (string)s["wheelchair"];
                var cuisine = (string)s["cuisine"];
                var veg = (string)s["veg"];
                var desc = string.Join("，", new[] { string.IsNullOrEmpty(cuisine) ? null : "料理類型：" + cuisine, string.IsNullOrEmpty(veg) ? null : veg }.Where(x => x != null));
                db.Stores.Add(new Store
                {
                    Name = (string)s["name"],
                    CategoryId = byName[(string)s["cat"]],
                    Latitude = (double)s["lat"],
                    Longitude = (double)s["lng"],
                    Address = string.IsNullOrEmpty((string)s["addr"]) ? "臺中市（地址未提供）" : (string)s["addr"],
                    Phone = string.IsNullOrEmpty((string)s["phone"]) ? null : (string)s["phone"],
                    OpenHours = string.IsNullOrEmpty((string)s["hours"]) ? null : (string)s["hours"],
                    Cuisine = string.IsNullOrEmpty(cuisine) ? null : cuisine,
                    Description = desc.Length == 0 ? null : desc,
                    StepFreeEntrance = wc == "yes",
                    HasRamp = false,
                    AccessibleToilet = false,
                    AccessibilityKnown = wc == "yes" || wc == "no" || wc == "limited",
                    AccessibilityVerified = false,
                    IsDemo = false,
                    DataSource = DataSources.Osm,
                    OsmId = (string)s["osm"],
                    Status = StoreStatus.Approved,
                    OwnerId = ownerId,
                    CreatedAt = now
                });
            }
            db.SaveChanges();
            db.Configuration.AutoDetectChangesEnabled = true;
        }
    }
}
