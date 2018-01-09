using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using codernews.Models;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;

namespace codernews.Controllers
{
    public class NewsController : Controller
    {
        public static List<News> model = new List<News>();
        private readonly IDistributedCache _distributedCache;
        private readonly IHostingEnvironment _environment;
        public NewsController(IDistributedCache distributedCache, IHostingEnvironment IHostingEnvironment)
        {
            _environment = IHostingEnvironment;
            _distributedCache = distributedCache;

            if (model.Count == 0)
            {
                model.AddRange(new News[]
                {
                new News(){
                ID = 1,
                Title = "Yeni yıldan ‘havalı’ beklentiler",
                Detail = @"İstanbul gibi Akdeniz ikliminin hâkim olduğu yerlerde en soğuk ay olan 
                        şubatta kar yağması kuvvetli bir ihtimal. Diğer zamanlarda da kısa süreli 
                        kar yağışları ve güneşli fakat hava kirliliği yüksek günler göreceğiz.",
                CreatedDate = DateTime.Now,
                Image="weather.jpg"
                },
                new News(){
                ID = 2,
                Title = "2018’in en popüler adresleri",
                Detail = @"Her yıl Türkiye’de seyahat edenlerin sayısı hızla artıyor. 2018’de de 
                           bu artışın devam etmesi bekleniyor. Artık yeni bir yıla giriyoruz. 2018’de nereye 
                           gideceğimize turizm acentaları ve biraz konunun uzmanı yazarlar karar veriyor.",
                CreatedDate = DateTime.Now,
                Image="place.jpg"
                },
                 new News(){
                ID = 3,
                Title = "Doğa insanları neden hep en mutlu ve en sağlıklı insanlardır?",
                Detail = @"Bilim adamları ağaçların yanında günde en az yarım saatini geçiren 
                         insanların öncelikle mental anlamda gelişim gösterdiğini ve bunun çok kısa sürede 
                         de fiziksel yansımalarının gerçekleştiğini söylüyor.",
                CreatedDate = DateTime.Now,
                Image="nature.jpg"
                },
                 new News(){
                ID = 4,
                Title = "Bilgisayarınızda virüs var mı? İşte anlamanın yolu",
                Detail = @"Son hastalığınız, sağlığınızla ilgili bazı sorunların 
                         işaretçisi olabilir. Benzer şekilde bilgisayarınıza bulaşan bir virüs, 
                         bir dizi işaret verebilir. Gördüğünüz tek bir işaret, virüsten kaynaklanmıyor 
                         olabilir ancak birkaç belirti, tehlike çanlarını duymanız gerektiği anlamına geliyor.",
                CreatedDate = DateTime.Now,
                Image="virus.jpg"
                }
                });
            }
        }
        public async Task<IActionResult> Index()
        {
            var data = await AddRedisCache(model, 30, "NewsData");
            data.ForEach(item=>item.UrlTitle=item.Title.FriendlyUrl());   
            return View(data);
        }

        public IActionResult Detail(string Title, int ID)
        {
            var data = model.FirstOrDefault(news => news.ID == ID);
            return View(data);
        }

        public async Task<List<News>> AddRedisCache(List<News> allData, int cacheTime, string cacheKey, bool force = false)
        {
            var dataNews = await _distributedCache.GetAsync(cacheKey);
            if (dataNews == null || force == true)
            {
                var data = JsonConvert.SerializeObject(allData);
                var dataByte = Encoding.UTF8.GetBytes(data);

                var option = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(cacheTime));
                option.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheTime);
                await _distributedCache.SetAsync(cacheKey, dataByte, option);
            }
            var newsString = await _distributedCache.GetStringAsync(cacheKey);
            return JsonConvert.DeserializeObject<List<News>>(newsString);
        }

        public IActionResult Admin()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SaveNews(News news, IFormFile NewsImage)
        {
            //Find News ID
            var MaxID = model.Max(max => max.ID);
            news.ID = MaxID + 1;
            news.UrlTitle = news.Title.FriendlyUrl();            
            //Upload Images
            if (NewsImage != null && NewsImage.Length > 0)
            {
                var fileName = ContentDispositionHeaderValue.Parse(NewsImage.ContentDisposition).FileName.Trim('"');
                var myUniqueFileName = Convert.ToString(Guid.NewGuid());

                var FileExtension = Path.GetExtension(fileName);
                var PureFileName = Path.GetFileNameWithoutExtension(fileName);
                var newFileName = PureFileName + '_' + myUniqueFileName + FileExtension;
                news.Image = newFileName; //New Image Name
                fileName = Path.Combine(_environment.WebRootPath, @"images") + $"/{newFileName}";
                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    await NewsImage.CopyToAsync(stream);
                }
            }   

            // Add to Model
            //model.ForEach(item=>item.UrlTitle=item.Title.FriendlyUrl());                   
            model.Insert(0, news);

             //Push signalR
            //Trigger The SignalR
            Connect().Wait();
            await connectionSignalR.InvokeAsync("PushNews", news);

            //Add Redis Cache
            var data = await AddRedisCache(model, 30, "NewsData", true);
            //Redirec To Index
            return RedirectToAction("Index");
        }
        static HubConnection connectionSignalR;
        public static async Task Connect()
        {
            connectionSignalR = new HubConnectionBuilder()
               .WithUrl("http://localhost:5000/newspush")
               .WithConsoleLogger()
               .Build();
            await connectionSignalR.StartAsync();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
