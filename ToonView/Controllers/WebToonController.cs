using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using View.Common.Models;
using View.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using View.Common.Models.Webtoon;
using MongoDB.Driver;
using MongoDB.Bson;
using HtmlAgilityPack;


namespace MapView.Controllers
{
    public class WebToonController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly ILogger<WebToonController> _logger;

        private IWebToonService _webToonService;
        

        public WebToonController(ILogger<WebToonController> logger, IConfiguration config,
                              IWebToonService webToonService)
        {
            _logger = logger;
            _configuration = config;
            _webToonService = webToonService;
            
        }



        public IActionResult Craw()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Crawling(int id, int no, string week, string status="Y")
        {
           var data = _webToonService.Crawling(id, no, week, status);

            return new JsonResult(data);
        }

        public IActionResult CrawList()
        {
            var webtoon = new Contents();
            var list = _webToonService.GetToonList(webtoon);

            return View(list);
        }

        [HttpPost]
        public IActionResult CrawList(int id)
        {
            var webtoon = new Contents();
            var list = _webToonService.GetToonList(webtoon);

            if (list != null)
            {
                list = list.Where(w => w.titleId == id).ToList();
            }

            return new JsonResult(list);
        }



        public IActionResult Index()
        {
            //ViewBag.Week = (int)DateTime.Now.DayOfWeek;

            return View();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="week"></param>
        /// <returns></returns>
        public IActionResult Week(int? week)
        {
            if(week == null)
            {
                week = (int)DateTime.Now.DayOfWeek;
            }

            ViewBag.Week = week;
            

            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="genre"></param>
        /// <returns></returns>
        public IActionResult Genre(string? genre)
        {
            ViewBag.Genre = genre;

            return View();
        }

        public IActionResult Finish()
        {
            ViewBag.Finish = "finish";
            return View();
        }


        public IActionResult Toon(int id, string? week, string? genre, string? finish)
        {
            var webtoon = new Contents();
            webtoon.titleId = id;

            var list = _webToonService.GetToonList(webtoon);
            webtoon = list.Where(w => w.titleId == id).ToList().First();

            webtoon.list = _webToonService.GetItemList(webtoon).OrderByDescending(o => o.no).ToList();


            ViewBag.Week = string.IsNullOrEmpty(week) ? "" : week;
            ViewBag.Genre = string.IsNullOrEmpty(genre) ? "" : genre;
            ViewBag.Finish = string.IsNullOrEmpty(finish) ? "" : finish;


            return View(webtoon);
        }




        [HttpPost]
        public IActionResult ToonList(string mode, string val)
        {
            var list = _webToonService.GetToonList(new Contents { });

            if (list != null)
            {
                if (mode == "w")
                    list = list.Where(w => w.week == val && w.status != "N").ToList();
                else if (mode == "g")
                    list = list.Where(w => w.genre.Contains(val) && w.status != "N").ToList();
                else if (mode == "f")
                {
                    list = list.Where(w => w.status == "N").ToList();
                }
            }

            foreach(var itm in list)
            {
                var items = _webToonService.GetItemList(new Contents { titleId = itm.titleId });

                itm.star = Math.Round(items.Sum(s => s.star) / items.Count(), 1).ToString();

                itm.lastUpdDate = items.OrderByDescending(o => o.no).First().regDate.ToString("yyyy.MM.dd");
            }


            return new JsonResult(list);
        }


        [HttpPost]
        public IActionResult ItemList(int id)
        {
            var list = _webToonService.GetItemList(new Contents { titleId = id });

            return new JsonResult(list);
        }


        
        public IActionResult Show(int id, int no, string? week, string? genre, string? finish)
        {
            var list = _webToonService.GetToonList(new Contents { titleId = id });
            var webtoon = list.Where(w => w.titleId == id).ToList().First();

            var data = _webToonService.GetItem(new ContentsList {
                titleId = id,
                no = no
            });

            webtoon.list.Add(data);


            ViewBag.Week = string.IsNullOrEmpty(week) ? "" : week;
            ViewBag.Genre = string.IsNullOrEmpty(genre) ? "" : genre;
            ViewBag.Finish = string.IsNullOrEmpty(finish) ? "" : finish;

            return View(webtoon);
        }


    }
}
