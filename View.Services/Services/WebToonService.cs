﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using View.Common.Models.Webtoon;
using View.Services.Database;
//using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using HtmlAgilityPack;




namespace View.Services
{
    public interface IWebToonService
    {
        ContentsList Crawling(int id, int no, string week, string status = "Y");
        void Crawling(Contents content, ContentsList list);


        List<Contents> GetToonList(Contents content);
        Contents GetToon(Contents content);

        List<ContentsList> GetItemList(Contents content);
        ContentsList GetItem(ContentsList content);

    }

        public class WebToonService : IWebToonService
    {

        private readonly IConfiguration _configuration;
        //private readonly IWebHostEnvironment _hostingEnvironment;
        
        //private readonly Redis _redis;
        private readonly MongoDb _mongoDB;

        public WebToonService(IConfiguration configuration)
        {
            _configuration = configuration;

            var host = _configuration.GetSection("MONGODB:SERVER").Value.ToString() + ":" + _configuration.GetSection("MONGODB:PORT").Value.ToString();

            _mongoDB = new MongoDb(_configuration.GetSection("MONGODB:USER").Value.ToString(),
                            host,
                            _configuration.GetSection("MONGODB:TOON_DB").Value.ToString());

        }
        public ContentsList Crawling(int titleId, int no, string week, string status = "Y")
        {
            var html = "https://comic.naver.com/webtoon/detail?titleId=" + titleId + "&no=" + no;
            HtmlWeb web = new HtmlWeb();

            var htmlDoc = web.Load(html);

            var node = htmlDoc.DocumentNode.SelectSingleNode("//head/title");

            var body = htmlDoc.DocumentNode.SelectSingleNode("//body/div");

            var container = body.SelectNodes("//div").Where(w => w.Id == "container").First();

            //var content = container.SelectSingleNode("//div");

            var doc = new HtmlDocument();
            doc.LoadHtml(container.InnerHtml);

            var doc2 = new HtmlDocument();
            var node2 = doc.DocumentNode.SelectNodes("//div/div").Where(w => w.Id == "sectionContWide").First();
            doc2.LoadHtml(node2.InnerHtml);

            // 썸네일 영역
            var thumb = doc2.DocumentNode.SelectNodes("//div/div")[0];
            var img = thumb.SelectSingleNode("//a/img");
            var src = img.Attributes["src"].Value;

            var detail = doc2.DocumentNode.SelectNodes("//div/div")[1];
            var title = thumb.SelectNodes("//h2/span")[0].InnerText;
            var artist = thumb.SelectNodes("//h2/span")[1].InnerText;
            var txt = thumb.SelectNodes("//p").Where(w => w.Attributes["class"].Value == "txt").First().InnerText;
            var genre = thumb.SelectNodes("//p/span")[0].InnerText;
            var age = thumb.SelectNodes("//p/span")[1].InnerText;
            //var age2 = age.SelectSingleNode("//span")[0];


            // 회차정보
            var listNo = doc2.DocumentNode.SelectNodes("//div/div")[1];
            var noName = listNo.SelectSingleNode("//div/h3").InnerText;
            var star = listNo.SelectNodes("//div/dl/dd/div/span").Where(w => w.Id == "topPointTotalNumber").First().InnerText;
            var regDt = listNo.SelectNodes("//div").Where(w => w.Attributes["class"].Value == "vote_lst").First()
                              .SelectNodes("dl").Where(w => w.Attributes["class"].Value == "rt").First()
                              .SelectNodes("dd").First().InnerText;

            // 웹툰 컨텐츠

            var view = doc.DocumentNode.SelectNodes("//div").Where(w => w.Id == "comic_view_area").First();

            var doc3 = new HtmlDocument();
            doc3.LoadHtml(view.InnerHtml);
            var content = doc3.DocumentNode.SelectNodes("//div")[0].InnerHtml;


            // 작가의말
            var doc4 = new HtmlDocument();
            var node3 = node2.NextSibling.NextSibling;
            doc4.LoadHtml(node3.InnerHtml);
            var artistNm = doc4.DocumentNode.SelectNodes("//div/div/h4/em/strong").First().InnerText;
            var comment = doc4.DocumentNode.SelectNodes("//div/div/p").First().InnerHtml;


            var webtoon = new Contents();
            webtoon.titleId = titleId;
            webtoon.titleName = title;
            webtoon.titleImg = src;
            webtoon.gubun = "webtoon";
            webtoon.artist = artist.Replace("\t", "");
            webtoon.thumImg = src;
            webtoon.desc = txt;
            webtoon.genre = genre;
            webtoon.age = age;
            webtoon.status = status;
            webtoon.week = week;


            var item = new ContentsList();
            item.titleId = titleId;
            item.no = no;
            item.name = noName;
            item.content = content.Replace("\t", "").Replace("\n", "");
            item.comment = comment;
            item.status = status;
            item.regDate = Convert.ToDateTime(regDt.Replace(".", "-"));
            item.star = float.Parse(star);
            //ViewBag.Item = item;


            this.Crawling(webtoon, item);

            return item;
        }

        public void Crawling(Contents content, ContentsList list)
        {

            var collection = _configuration.GetSection("MONGODB:TOON_CONTENTS").Value;
            var collectionList = _configuration.GetSection("MONGODB:TOON_CONTELTSLIST").Value;

            var doc = _mongoDB.GetData<Contents>(content.titleId, collection);
            if(doc == null)
            {
                // ins
                _mongoDB.InsData<Contents>(content, collection);

            }

            var docList = _mongoDB.GetData<ContentsList>(list.titleId, list.no, collectionList);
            if (docList == null)
            {
                _mongoDB.InsData<ContentsList>(list, collectionList);
            }

        }


        public List<Contents> GetToonList(Contents content)
        {
            var collection = _configuration.GetSection("MONGODB:TOON_CONTENTS").Value;
            var list = _mongoDB.DataList<Contents>(collection);

            return list;
        }

        public Contents GetToon(Contents content)
        {
            var collection = _configuration.GetSection("MONGODB:TOON_CONTENTS").Value;
            var list = _mongoDB.GetData<Contents>(content.titleId, collection);

            return list;
        }


        public List<ContentsList> GetItemList(Contents content)
        {
            var collection = _configuration.GetSection("MONGODB:TOON_CONTELTSLIST").Value;
            var list = _mongoDB.DataListByTitleId<ContentsList>(collection, content.titleId);

            return list;
        }

        public ContentsList GetItem(ContentsList content)
        {
            var collection = _configuration.GetSection("MONGODB:TOON_CONTELTSLIST").Value;
            var list = _mongoDB.GetData<ContentsList>(content.titleId, content.no, collection);

            return list;
        }
    }
}
