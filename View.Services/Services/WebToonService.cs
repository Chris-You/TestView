using System;
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

            var contentNode = body.SelectNodes("//div").Where(w => w.Id == "content").First();
            var sectionContWide = body.SelectNodes("//div").Where(w => w.Id == "sectionContWide").First();

            var doc = new HtmlDocument();
            doc.LoadHtml(sectionContWide.InnerHtml);

            // 썸네일 영역
            var thumb = doc.DocumentNode.SelectNodes("//div/div").Where(w=> w.Attributes["class"].Value == "thumb").First();
            var src = thumb.SelectSingleNode("//a/img").Attributes["src"].Value;

            // 상서젱보 영역
            var detail = doc.DocumentNode.SelectNodes("//div/div").Where(w => w.Attributes["class"].Value == "detail").First();
            var title = detail.SelectNodes("//h2/span").Where(w => w.Attributes["class"].Value == "title").First().InnerText;
            var artist = detail.SelectNodes("//h2/span").Where(w => w.Attributes["class"].Value == "wrt_nm").First().InnerText;
            var txt = detail.SelectNodes("//p").Where(w => w.Attributes["class"].Value == "txt").First().InnerText;
            var genre = detail.SelectNodes("//p/span").Where(w => w.Attributes["class"].Value == "genre").First().InnerText;
            var age = detail.SelectNodes("//p/span").Where(w => w.Attributes["class"].Value == "age").First().InnerText;

            // 회차정보
            var listNo = doc.DocumentNode.SelectNodes("//div").Where(w => w.Attributes["class"].Value == "tit_area").First();
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
            var doc2 = new HtmlDocument();
            var contnextnode = doc.DocumentNode;
            doc2.LoadHtml(contentNode.InnerHtml);
            var artistNm = doc2.DocumentNode.SelectNodes("//div/div/h4/em/strong").First().InnerText;
            var comment = doc2.DocumentNode.SelectNodes("//div/div/p").First().InnerHtml;


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
