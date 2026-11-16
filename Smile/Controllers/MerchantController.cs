using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Smile.Infrastructure;
using Smile.Models;
using Smile.ViewModels;

namespace Smile.Controllers
{
    /// <summary>店家後台：主控台、店家資料、商品與服務、優惠、掃碼核銷</summary>
    [Authorize(Roles = AppRoles.Merchant + "," + AppRoles.Admin)]
    public class MerchantController : BaseController
    {
        private IQueryable<Store> OwnedStores()
        {
            var uid = CurrentUserId.Value;
            return db.Stores.Where(s => s.OwnerId == uid);
        }

        private Store FindOwned(int? storeId)
        {
            var stores = OwnedStores().OrderBy(s => s.Id);
            return storeId.HasValue ? stores.FirstOrDefault(s => s.Id == storeId.Value) : stores.FirstOrDefault();
        }

        private void LoadStoreLists(Store store)
        {
            ViewBag.CategoryId = new SelectList(db.StoreCategories.OrderBy(c => c.SortOrder).ToList(), "Id", "Name", store == null ? (int?)null : store.CategoryId);
        }

        // 店家主控台
        public ActionResult Index(int? storeId)
        {
            var stores = OwnedStores().Include("Category").OrderBy(s => s.Id).ToList();
            if (!stores.Any()) return RedirectToAction("CreateStore");
            var current = stores.FirstOrDefault(s => s.Id == storeId) ?? stores.First();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            // 近 7 天每天的領券數與核銷數
            var weekStart = today.AddDays(-6);
            var claimTimes = db.UserCoupons.Where(u => u.Coupon.StoreId == current.Id && u.ClaimedAt >= weekStart).Select(u => u.ClaimedAt).ToList();
            var redeemTimes = db.Redemptions.Where(r => r.StoreId == current.Id && r.RedeemedAt >= weekStart).Select(r => r.RedeemedAt).ToList();
            var week = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).Select(d => new DayStat
            {
                Date = d,
                Claims = claimTimes.Count(t => t.Date == d),
                Redemptions = redeemTimes.Count(t => t.Date == d)
            }).ToList();

            return View(new MerchantDashboardViewModel
            {
                Stores = stores,
                CurrentStore = current,
                TodayRedemptions = db.Redemptions.Count(r => r.StoreId == current.Id && r.RedeemedAt >= today && r.RedeemedAt < tomorrow),
                ActiveCoupons = db.Coupons.Count(c => c.StoreId == current.Id && c.IsActive && c.EndDate >= today),
                MonthClaims = db.UserCoupons.Count(u => u.Coupon.StoreId == current.Id && u.ClaimedAt >= monthStart),
                RecentRedemptions = db.Redemptions.Include("UserCoupon.Coupon").Where(r => r.StoreId == current.Id)
                    .OrderByDescending(r => r.RedeemedAt).Take(5).ToList(),
                Week = week,
                Favorites = db.Favorites.Count(f => f.StoreId == current.Id),
                StampCards = db.StampCards.Count(c => c.StoreId == current.Id)
            });
        }

        // 申請新店家（送出後待管理者審核）
        public ActionResult CreateStore()
        {
            var store = new Store { Latitude = 24.1478, Longitude = 120.6862, PaymentMethods = "現金" };
            LoadStoreLists(store);
            return View(store);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CreateStore([Bind(Include = "Name,Description,OwnerMessage,Address,Phone,OpenHours,Latitude,Longitude,PaymentMethods,StepFreeEntrance,HasRamp,AccessibleToilet,CategoryId")] Store store)
        {
            ModelState.Remove("Status");
            if (!ModelState.IsValid) { LoadStoreLists(store); return View(store); }
            store.OwnerId = CurrentUserId.Value;
            store.Status = StoreStatus.Pending;
            store.AccessibilityVerified = false;
            store.IsDemo = false;
            store.DataSource = DataSources.Merchant;
            store.AccessibilityKnown = true;
            store.CreatedAt = DateTime.Now;
            db.Stores.Add(store);
            db.SaveChanges();
            Flash("已送出申請，管理者審核通過後就會上架。");
            return RedirectToAction("Index", new { storeId = store.Id });
        }

        public ActionResult EditStore(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            LoadStoreLists(store);
            return View(store);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult EditStore(int id, FormCollection form)
        {
            var store = FindOwned(id);
            if (store == null) return HttpNotFound();
            if (TryUpdateModel(store, "", new[] { "Name", "Description", "Address", "Phone", "OpenHours", "Latitude", "Longitude", "PaymentMethods", "StepFreeEntrance", "HasRamp", "AccessibleToilet", "CategoryId", "OwnerMessage" }))
            {
                // 店家自行修改無障礙資訊後，需重新查證
                store.AccessibilityVerified = false;
                store.AccessibilityKnown = true;
                db.SaveChanges();
                Flash("店家資料已更新");
                return RedirectToAction("Index", new { storeId = store.Id });
            }
            LoadStoreLists(store);
            return View(store);
        }

        // ===== 店家照片：讓顧客看到真實的店面與老闆 =====
        public ActionResult Photo(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            return View(store);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Photo(int storeId, System.Web.HttpPostedFileBase photo)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            var ext = photo == null ? "" : System.IO.Path.GetExtension(photo.FileName ?? "").ToLowerInvariant();
            if (photo == null || photo.ContentLength == 0) { Flash("請選擇照片", "warning"); return RedirectToAction("Photo", new { storeId }); }
            if (photo.ContentLength > 3 * 1024 * 1024) { Flash("照片不能超過 3 MB", "warning"); return RedirectToAction("Photo", new { storeId }); }
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") { Flash("只接受 JPG 或 PNG", "warning"); return RedirectToAction("Photo", new { storeId }); }
            try
            {
                // 確認真的是圖片（不只看副檔名）
                using (var img = System.Drawing.Image.FromStream(photo.InputStream, false, true)) { }
            }
            catch (ArgumentException) { Flash("這不是有效的圖片檔", "warning"); return RedirectToAction("Photo", new { storeId }); }

            var dir = Server.MapPath("~/Uploads/stores");
            System.IO.Directory.CreateDirectory(dir);
            var name = store.Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ext;
            photo.InputStream.Position = 0;
            photo.SaveAs(System.IO.Path.Combine(dir, name));
            store.PhotoUrl = "~/Uploads/stores/" + name;
            db.SaveChanges();
            Flash("照片已更新");
            return RedirectToAction("Photo", new { storeId });
        }

        // ===== 店頭立牌：印出來貼在門口，客人掃 QR Code 就到店家頁領券 =====
        public ActionResult Poster(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            var today = DateTime.Today;
            ViewBag.StoreUrl = Url.Action("Details", "Stores", new { id = store.Id }, Request.Url.Scheme);
            ViewBag.CouponCount = db.Coupons.Count(c => c.StoreId == store.Id && c.IsActive && c.EndDate >= today);
            return View(store);
        }

        /// <summary>優惠券表單的「參加活動」選單：進行中或即將開始的活動</summary>
        private void LoadCampaigns()
        {
            var today = DateTime.Today;
            ViewBag.Campaigns = db.Campaigns.Where(c => c.IsActive && c.EndDate >= today).OrderBy(c => c.StartDate).ToList();
        }

        // ===== 商品與服務 =====
        public ActionResult Products(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            ViewBag.Store = store;
            return View(db.Products.Where(p => p.StoreId == store.Id).OrderBy(p => p.Name).ToList());
        }

        public ActionResult CreateProduct(int storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            ViewBag.Store = store;
            return View("ProductForm", new Product { StoreId = store.Id, IsAvailable = true });
        }

        public ActionResult EditProduct(int id)
        {
            var uid = CurrentUserId.Value;
            var product = db.Products.Include("Store").FirstOrDefault(p => p.Id == id && p.Store.OwnerId == uid);
            if (product == null) return HttpNotFound();
            ViewBag.Store = product.Store;
            return View("ProductForm", product);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveProduct([Bind(Include = "Id,StoreId,Name,Price,Description,IsAvailable")] Product product)
        {
            var store = FindOwned(product.StoreId);
            if (store == null) return HttpNotFound();
            if (!ModelState.IsValid) { ViewBag.Store = store; return View("ProductForm", product); }

            if (product.Id == 0) db.Products.Add(product);
            else
            {
                var existing = db.Products.FirstOrDefault(p => p.Id == product.Id && p.StoreId == store.Id);
                if (existing == null) return HttpNotFound();
                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.Description = product.Description;
                existing.IsAvailable = product.IsAvailable;
            }
            db.SaveChanges();
            Flash("已儲存「" + product.Name + "」");
            return RedirectToAction("Products", new { storeId = store.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            var uid = CurrentUserId.Value;
            var product = db.Products.Include("Store").FirstOrDefault(p => p.Id == id && p.Store.OwnerId == uid);
            if (product == null) return HttpNotFound();
            var storeId = product.StoreId;
            db.Products.Remove(product);
            db.SaveChanges();
            Flash("已刪除");
            return RedirectToAction("Products", new { storeId });
        }

        // ===== 優惠管理 =====
        public ActionResult Coupons(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            ViewBag.Store = store;
            return View(db.Coupons.Where(c => c.StoreId == store.Id).OrderByDescending(c => c.EndDate).ToList());
        }

        public ActionResult CreateCoupon(int storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            ViewBag.Store = store;
            LoadCampaigns();
            return View("CouponForm", new Coupon { StoreId = store.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(30), TotalQuantity = 50, IsActive = true });
        }

        public ActionResult EditCoupon(int id)
        {
            var uid = CurrentUserId.Value;
            var coupon = db.Coupons.Include("Store").FirstOrDefault(c => c.Id == id && c.Store.OwnerId == uid);
            if (coupon == null) return HttpNotFound();
            ViewBag.Store = coupon.Store;
            LoadCampaigns();
            return View("CouponForm", coupon);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SaveCoupon([Bind(Include = "Id,StoreId,Title,Description,StartDate,EndDate,TotalQuantity,IsActive,CampaignId")] Coupon coupon)
        {
            var store = FindOwned(coupon.StoreId);
            if (store == null) return HttpNotFound();
            if (coupon.EndDate < coupon.StartDate) ModelState.AddModelError("EndDate", "結束日期不能早於開始日期");
            // 只能參加上架中的活動
            if (coupon.CampaignId.HasValue && !db.Campaigns.Any(c => c.Id == coupon.CampaignId.Value && c.IsActive)) coupon.CampaignId = null;

            Coupon existing = null;
            if (coupon.Id != 0)
            {
                existing = db.Coupons.FirstOrDefault(c => c.Id == coupon.Id && c.StoreId == store.Id);
                if (existing == null) return HttpNotFound();
                if (coupon.TotalQuantity < existing.IssuedCount) ModelState.AddModelError("TotalQuantity", "發行張數不能少於已領取的 " + existing.IssuedCount + " 張");
            }
            if (!ModelState.IsValid) { ViewBag.Store = store; LoadCampaigns(); return View("CouponForm", coupon); }

            if (existing == null) { coupon.IssuedCount = 0; db.Coupons.Add(coupon); }
            else
            {
                existing.Title = coupon.Title;
                existing.Description = coupon.Description;
                existing.StartDate = coupon.StartDate;
                existing.EndDate = coupon.EndDate;
                existing.TotalQuantity = coupon.TotalQuantity;
                existing.IsActive = coupon.IsActive;
                existing.CampaignId = coupon.CampaignId;
            }
            db.SaveChanges();
            Flash("已儲存優惠「" + coupon.Title + "」");
            return RedirectToAction("Coupons", new { storeId = store.Id });
        }

        // 已有人領取的優惠不刪除，只下架，避免使用者手上的券失效
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteCoupon(int id)
        {
            var uid = CurrentUserId.Value;
            var coupon = db.Coupons.Include("Store").FirstOrDefault(c => c.Id == id && c.Store.OwnerId == uid);
            if (coupon == null) return HttpNotFound();
            var storeId = coupon.StoreId;
            if (db.UserCoupons.Any(u => u.CouponId == id)) { coupon.IsActive = false; Flash("已有人領取，改為下架", "info"); }
            else { db.Coupons.Remove(coupon); Flash("已刪除優惠"); }
            db.SaveChanges();
            return RedirectToAction("Coupons", new { storeId });
        }

        // ===== 掃碼核銷 =====
        public ActionResult Redeem(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            ViewBag.Store = store;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Redeem(int storeId, string code)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            ViewBag.Store = store;
            UserCoupon uc;
            var error = TryRedeem(store, code, out uc);
            if (error != null) ViewBag.Error = error;
            else ViewBag.Success = "核銷成功：「" + uc.Coupon.Title + "」";
            return View();
        }

        /// <summary>核銷優惠券，成功回傳 null，失敗回傳原因</summary>
        private string TryRedeem(Store store, string code, out UserCoupon uc)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length > 0 && !code.StartsWith("SMILE-")) code = "SMILE-" + code;
            uc = db.UserCoupons.Include("Coupon").Include("User").FirstOrDefault(u => u.Code == code);

            if (uc == null || uc.Coupon.StoreId != store.Id)
                return "查無此核銷碼，或這張券不屬於「" + store.Name + "」。";
            if (uc.Status == CouponStatus.Used)
                return "這張優惠券已經在 " + (uc.UsedAt.HasValue ? uc.UsedAt.Value.ToString("yyyy-MM-dd HH:mm") : "先前") + " 使用過了。";
            if (uc.Coupon.EndDate < DateTime.Today)
                return "這張優惠券已過期（" + uc.Coupon.EndDate.ToString("yyyy-MM-dd") + "）。";

            var now = DateTime.Now;
            uc.Status = CouponStatus.Used;
            uc.UsedAt = now;
            db.Redemptions.Add(new Redemption { UserCouponId = uc.Id, StoreId = store.Id, RedeemedById = CurrentUserId.Value, RedeemedAt = now });
            db.SaveChanges();
            return null;
        }

        // ===== POS 收銀模式：全螢幕、數字鍵盤，掃優惠券或會員卡 =====
        public ActionResult Pos(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var model = new PosViewModel
            {
                Store = store,
                Stores = OwnedStores().OrderBy(s => s.Id).ToList(),
                Message = TempData["PosMessage"] as string,
                IsError = TempData["PosError"] != null,
                TodayRedemptions = db.Redemptions.Count(r => r.StoreId == store.Id && r.RedeemedAt >= today && r.RedeemedAt < tomorrow),
                TodayStamps = db.StampRecords.Where(r => r.StampCard.StoreId == store.Id && r.Kind == StampKinds.Stamp && r.CreatedAt >= today && r.CreatedAt < tomorrow).Select(r => (int?)r.Change).Sum() ?? 0,
                TodayRewards = db.StampRecords.Count(r => r.StampCard.StoreId == store.Id && r.Kind == StampKinds.Reward && r.CreatedAt >= today && r.CreatedAt < tomorrow)
            };

            var memberCode = TempData["PosMember"] as string;
            if (memberCode != null)
            {
                model.Member = db.Users.FirstOrDefault(u => u.MemberCode == memberCode);
                if (model.Member != null)
                {
                    var mid = model.Member.Id;
                    model.Card = db.StampCards.FirstOrDefault(c => c.UserId == mid && c.StoreId == store.Id);
                }
            }

            var log = db.Redemptions.Include("UserCoupon.Coupon").Where(r => r.StoreId == store.Id && r.RedeemedAt >= today && r.RedeemedAt < tomorrow)
                .OrderByDescending(r => r.RedeemedAt).Take(10).ToList()
                .Select(r => new PosLogItem { Time = r.RedeemedAt, Icon = "🎟️", Text = "核銷「" + r.UserCoupon.Coupon.Title + "」" }).ToList();
            log.AddRange(db.StampRecords.Include("StampCard.User").Where(r => r.StampCard.StoreId == store.Id && r.CreatedAt >= today && r.CreatedAt < tomorrow)
                .OrderByDescending(r => r.CreatedAt).Take(10).ToList()
                .Select(r => new PosLogItem
                {
                    Time = r.CreatedAt,
                    Icon = r.Kind == StampKinds.Reward ? "🎁" : "⭐",
                    Text = (r.Kind == StampKinds.Reward ? "兌換獎勵" : "蓋章 +" + r.Change) + "（" + r.StampCard.User.DisplayName + "）"
                }));
            model.Log = log.OrderByDescending(l => l.Time).Take(12).ToList();
            return View(model);
        }

        private ActionResult PosResult(int storeId, string message, bool error, string memberCode = null)
        {
            TempData["PosMessage"] = message;
            if (error) TempData["PosError"] = true;
            if (memberCode != null) TempData["PosMember"] = memberCode;
            return RedirectToAction("Pos", new { storeId });
        }

        /// <summary>掃描或輸入：10 位數字是會員卡，其餘當作優惠券核銷碼</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PosScan(int storeId, string code)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            code = (code ?? "").Trim().Replace(" ", "");
            if (code.Length == 0) return PosResult(store.Id, "請先掃描或輸入號碼。", true);

            if (code.Length == 10 && code.All(char.IsDigit))
            {
                if (!db.Users.Any(u => u.MemberCode == code)) return PosResult(store.Id, "查無此會員編號。", true);
                return PosResult(store.Id, store.StampGoal > 0 ? "已讀取會員卡，請選擇要蓋幾點。" : "已讀取會員卡。這間店還沒開啟集點卡，可以到「集點卡設定」開啟。", false, code);
            }

            UserCoupon uc;
            var error = TryRedeem(store, code, out uc);
            if (error != null) return PosResult(store.Id, error, true);
            // 核銷成功後順便帶出這位會員，店員可以直接蓋章
            return PosResult(store.Id, "核銷成功：「" + uc.Coupon.Title + "」", false, store.StampGoal > 0 ? uc.User.MemberCode : null);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PosStamp(int storeId, string memberCode, int count = 1)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            if (store.StampGoal <= 0) return PosResult(store.Id, "這間店還沒開啟集點卡。", true);
            if (count < 1 || count > 5) return PosResult(store.Id, "一次只能蓋 1 到 5 點。", true, memberCode);
            var member = db.Users.FirstOrDefault(u => u.MemberCode == memberCode);
            if (member == null) return PosResult(store.Id, "查無此會員編號。", true);

            var now = DateTime.Now;
            var card = db.StampCards.FirstOrDefault(c => c.UserId == member.Id && c.StoreId == store.Id);
            if (card == null)
            {
                card = new StampCard { UserId = member.Id, StoreId = store.Id, CreatedAt = now };
                db.StampCards.Add(card);
            }
            card.Stamps += count;
            card.UpdatedAt = now;
            db.StampRecords.Add(new StampRecord { StampCard = card, Kind = StampKinds.Stamp, Change = count, OperatorId = CurrentUserId.Value, CreatedAt = now });
            db.SaveChanges();
            var msg = "已蓋 " + count + " 點，目前 " + card.Stamps + " / " + store.StampGoal + " 點";
            if (card.Stamps >= store.StampGoal) msg += "，可以兌換「" + store.StampReward + "」了！";
            return PosResult(store.Id, msg, false, memberCode);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult PosReward(int storeId, string memberCode)
        {
            var store = FindOwned(storeId);
            if (store == null) return HttpNotFound();
            var member = db.Users.FirstOrDefault(u => u.MemberCode == memberCode);
            if (member == null) return PosResult(store.Id, "查無此會員編號。", true);
            var card = db.StampCards.FirstOrDefault(c => c.UserId == member.Id && c.StoreId == store.Id);
            if (store.StampGoal <= 0 || card == null || card.Stamps < store.StampGoal)
                return PosResult(store.Id, "點數還不夠，無法兌換。", true, memberCode);

            var now = DateTime.Now;
            card.Stamps -= store.StampGoal;
            card.RewardsRedeemed++;
            card.UpdatedAt = now;
            db.StampRecords.Add(new StampRecord { StampCardId = card.Id, Kind = StampKinds.Reward, Change = -store.StampGoal, OperatorId = CurrentUserId.Value, CreatedAt = now });
            db.SaveChanges();
            return PosResult(store.Id, "已兌換「" + store.StampReward + "」，剩 " + card.Stamps + " 點。", false, memberCode);
        }

        // ===== 集點卡設定 =====
        public ActionResult Stamps(int? storeId)
        {
            var store = FindOwned(storeId);
            if (store == null) return RedirectToAction("Index");
            return View(BuildStampSettings(store, new StampSettingsViewModel { StampGoal = store.StampGoal, StampReward = store.StampReward }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Stamps(StampSettingsViewModel model)
        {
            var store = FindOwned(model.StoreId);
            if (store == null) return HttpNotFound();
            if (model.StampGoal > 0 && string.IsNullOrWhiteSpace(model.StampReward))
                ModelState.AddModelError("StampReward", "開啟集點時，請填寫集滿可以兌換什麼");
            if (!ModelState.IsValid) return View(BuildStampSettings(store, model));

            store.StampGoal = model.StampGoal;
            store.StampReward = model.StampGoal > 0 ? model.StampReward.Trim() : null;
            db.SaveChanges();
            Flash(store.StampGoal > 0 ? "集點卡已開啟：集滿 " + store.StampGoal + " 點換「" + store.StampReward + "」" : "已關閉集點卡，顧客手上的點數會保留。");
            return RedirectToAction("Stamps", new { storeId = store.Id });
        }

        private StampSettingsViewModel BuildStampSettings(Store store, StampSettingsViewModel model)
        {
            model.StoreId = store.Id;
            model.Store = store;
            model.CardCount = db.StampCards.Count(c => c.StoreId == store.Id);
            model.TotalStamps = db.StampRecords.Where(r => r.StampCard.StoreId == store.Id && r.Kind == StampKinds.Stamp).Select(r => (int?)r.Change).Sum() ?? 0;
            model.TotalRewards = db.StampRecords.Count(r => r.StampCard.StoreId == store.Id && r.Kind == StampKinds.Reward);
            return model;
        }
    }
}
