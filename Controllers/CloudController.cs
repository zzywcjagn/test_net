﻿using IServer.IPageServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NETJDC.Extensions;
using NETJDC.Request;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Systems;

namespace NETJDC.Controllers
{
    [Route("api/")]
    [ApiController, EnableCors("CorsPolicy")]
    public class CloudController : ControllerBase
    {

        private readonly IPageServer _PageServer;
        private readonly MainConfig _mainConfig;
        public CloudController(IPageServer helper, MainConfig mainConfig)
        {
            _PageServer = helper;
            _mainConfig = mainConfig;
        }
        [HttpGet, Route("Version")]
        public IActionResult Version()
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            result.data = new { Version = _mainConfig.Version };
            return Ok(result);
        }
        [HttpGet, Route("Title")]
        public IActionResult Title()
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            var Title = _mainConfig.Title;
            if (string.IsNullOrEmpty(_mainConfig.Title)) Title = "NolanJDCloud";
            result.data = new { title = Title };
            return Ok(result);
        }
        [HttpGet, Route("Config")]
        public async Task<IActionResult> Config()
        {
            var list = new List<Qlitem>();
            var type = Enum.GetName(typeof(UpTypeEum), _mainConfig.UPTYPE);
            var ckcount = 0;
            var wskeycount = 0;
            if (_mainConfig.Config.Count>0)
            {
                list = _mainConfig.Config.Select(x => new Qlitem { QLkey = x.QLkey, QLName = x.QLName, QL_CAPACITY = x.QL_CAPACITY }).ToList();
                var config = _mainConfig.Config.First();
                var qlcount = await config.GetEnvsCount();
                var qlwscount = await config.GetEnvsWSKEYCount();
                ckcount = config.QL_CAPACITY - qlcount;
                wskeycount= config.QL_CAPACITY - qlwscount;
                if (ckcount < 0) ckcount = 0;
                if (wskeycount < 0) wskeycount = 0;
            }
            
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            int closetime = int.Parse(_mainConfig.Closetime);
            string MaxTab = _mainConfig.MaxTab;
            string Announcement = _mainConfig.Announcement;
            var intabcount= _PageServer.GetPageCount();
            int AutoCaptchaCout = int.Parse(_mainConfig.AutoCaptchaCount);
            int tabcount = int.Parse(MaxTab) - intabcount;
            result.data = new { type= type, list =list, closetime= closetime, autocount = AutoCaptchaCout, ckcount = ckcount , tabcount = tabcount , announcement = Announcement, wskeycount= wskeycount };
            return Ok(result);
        }
        [HttpGet, Route("QLConfig")]
        public async Task<IActionResult> QLConfig(int qlkey)
        {
            var ckcount = 0;
            var wskeycount = 0;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            if (_mainConfig.UPTYPE == UpTypeEum.ql)
            {
                var config = _mainConfig.GetConfig(qlkey);
                var qlcount = await config.GetEnvsCount();
                ckcount = config.QL_CAPACITY - qlcount;
                var qlwscount = await config.GetEnvsWSKEYCount();
                ckcount = config.QL_CAPACITY - qlcount;
                wskeycount = config.QL_CAPACITY - qlwscount;
                if (ckcount < 0) ckcount = 0;
                if (wskeycount < 0) wskeycount = 0;
            }
            string MaxTab = _mainConfig.MaxTab;
            var intabcount = _PageServer.GetPageCount();
            int tabcount = int.Parse(MaxTab) - intabcount;
            if (tabcount < 0) tabcount = 0;
            if (ckcount < 0) ckcount = 0;
            result.data = new { ckcount = ckcount, tabcount =tabcount, wskeycount = wskeycount };
            return Ok(result);
        }
  
        [HttpPost, Route("AutoCaptcha")]
        public async Task<IActionResult> AutoCaptcha(RequestEntity obj)
        {
            string Phone = obj.Phone;
            if (string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
            if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
            ResultModel<object> result = await _PageServer.AutoCaptcha(Phone);
            return Ok(result);
        }
        [HttpPost, Route("SendSMS")]
        public async Task<IActionResult> SendSMS(RequestEntity obj)
        {

            string Phone = obj.Phone;
            int qlkey =  obj.qlkey;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
           
            try
            {
               
                if (string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
                if (_mainConfig.UPTYPE == UpTypeEum.ql&&qlkey == 0) throw new Exception("请选择服务器");
              
                if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
                await _PageServer.PageClose(Phone);
                result = await _PageServer.OpenJDTab(qlkey, Phone, _mainConfig.UPTYPE == UpTypeEum.ql);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (!string.IsNullOrEmpty(Phone))
                {
                    await _PageServer.PageClose(Phone);
                }
                  
                result.data = new { Status = 404 };
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpGet, Route("User")]
        public async Task<IActionResult> Userd(string qlid,int qlkey)
        {
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            try
            {
                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("服务器为空");
                var config = _mainConfig.GetConfig(qlkey);
                var env = await config.GetEnvbyid(qlid);
                if (env == null) throw new Exception("未找到相应的账号请检查");
                var timestamp = env["timestamp"].ToString();
                var remarks = env["remarks"].ToString();
                var nickname = "";
                if(env["name"].ToString()== "JD_COOKIE")
                     nickname = await GetNickname(env["value"].ToString());
                if (env["name"].ToString() == "JD_WSCK")
                {
                    var ck =await _PageServer.WSkeyGetToken(env["value"].ToString());
                    nickname = await GetNickname(ck);
                }
                result.data = new { qlid = qlid, qlkey = qlkey,ck= env["value"].ToString(), timestamp = timestamp, remarks = remarks , nickname=nickname,qrurl= config.QRurl};
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        private async Task<string> GetNickname(string cookie)
        {
            try
            {
                TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);
                var url = @"https://twilight-snowflake-3fa4.mazehao.workers.dev";
                using (HttpClient client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    var result = await client.GetAsync(url);
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine("获取nickname");
                    //JObject j = JObject.Parse(resultContent);
                    // data?.userInfo.baseInfo.nickname
                    return resultContent;
                    //return j["data"]["userInfo"]["baseInfo"]["nickname"].ToString();
                }
            }
            catch (Exception e)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {

                        client.DefaultRequestHeaders.Add("Cookie", cookie);
                        var result = await client.GetAsync("https://twilight-snowflake-3fa4.mazehao.workers.dev");
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        Console.WriteLine("获取nickname");
                        //JObject j = JObject.Parse(resultContent);
                        return resultContent;
                        // data?.userInfo.baseInfo.nickname
                        //return j["data"]["userInfo"]["baseInfo"]["nickname"].ToString();


                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString()); ;
                    return "未知";
                }

            }

        }
        [HttpPost, Route("Upremarks")]
        public async Task<IActionResult> Upremarks(Requestremarks obj)
        {
            string qlid = obj.qlid;
            int qlkey = obj.qlkey;
            string remarks = obj.remarks;
            ResultModel<JObject> result = ResultModel<JObject>.Create(true, "");

            try
            {
                if (string.IsNullOrEmpty(remarks)) throw new Exception("备注为空");
                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("请选择服务器");
                var config = _mainConfig.GetConfig(qlkey);
                var env =await config.GetEnvbyid(qlid);
                if (env == null) throw new Exception("未找到相应的账号请检查");
               var upresult = await config.UpdateEnv(env["value"].ToString(),qlid, env["name"].ToString(), remarks);
                var  timestamp = upresult.data["timestamp"].ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpPost, Route("del")]
        public async Task<IActionResult> del(RequestDEL obj)
        {
            string qlid = obj.qlid;
            int qlkey = obj.qlkey;
            ResultModel<JObject> result = ResultModel<JObject>.Create(true, "");

            try
            {

                if (string.IsNullOrEmpty(qlid)) throw new Exception("Id为空");
                if (qlkey == 0) throw new Exception("请选择服务器");
                var config = _mainConfig.GetConfig(qlkey);
                var env = await config.GetEnvbyid(qlid);
                if (env == null) throw new Exception("未找到相应的账号请检查");
                // var Nickname = await GetNickname(env["value"].ToString());
                var Nickname = "";
                var type = "";
                if (env["name"].ToString() == "JD_COOKIE")
                    Nickname = await GetNickname(env["value"].ToString());
                if (env["name"].ToString() == "JD_WSCK")
                {
                    type = "WSCK";
                    var ck = await _PageServer.WSkeyGetToken(env["value"].ToString());
                    Nickname = await GetNickname(ck);
                }
                if (env["remarks"] != null)
                    Nickname = env["remarks"].ToString();
                string pattern = @"pin=(.*)";
                Match m = Regex.Match(env["value"].ToString(), pattern);
                var pt_pin = m.Groups[1].ToString();
                result = await config.DelEnv(qlid);
                await _mainConfig.pushPlusNotify(@" 服务器;" + config.QLkey + " " + config.QLName + "  <br> "+ type + "用户 " + Nickname + "   " + pt_pin + " 删除CK 跑路了");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
              
                result.message = e.Message;
                result.success = false;
            }
            return Ok(result);
        }
        [HttpPost, Route("UploadWSKEY")]
        public async Task<IActionResult> UploadWSKEY(RequestWSKEY obj)
        {
            ResultModel<object> result = ResultModel<object>.Create(false, "");
            string wskey = obj.wskey;
            int qlkey = obj.qlkey;
            string remarks = obj.remarks;
            if (string.IsNullOrEmpty(remarks)) throw new Exception("备注为空");
            if (string.IsNullOrEmpty(wskey)) throw new Exception("wskey为空");
            if (qlkey == 0) throw new Exception("请选择服务器");
            var config = _mainConfig.GetConfig(qlkey);
            var ck = await _PageServer.WSkeyGetToken(wskey);
            var Nickname = await GetNickname(ck);
            int MAXCount = config.QL_CAPACITY;
            JArray data = await config.GetEnv();
            JToken env = null;
            var QLCount = await config.GetEnvsWSKEYCount(); ;
            string pattern = @"pin=(.*?);";
            Match m = Regex.Match(wskey, pattern);
            var pin = m.Groups[1].ToString();
            if (data != null)
            {
                env = data.FirstOrDefault(x => x["name"].ToString()== "JD_WSCK" && x["value"].ToString().Contains("pin=" + pin + ";"));
            }
            string QLId = "";
            string timestamp = "";
            if (env == null)
            {
                if (QLCount >= MAXCount)
                {
                    result.message = "你来晚了，没有多余的位置了";
                    result.data = new { Status = 501 };
                }

                var addresult = await config.AddEnv(wskey, "JD_WSCK", remarks);
                JObject addUser = (JObject)addresult.data[0];
                QLId = addUser["_id"].ToString();
                timestamp = addUser["timestamp"].ToString();

                await _mainConfig.pushPlusNotify(@" 服务器;" + config.QLkey + " " + config.QLName + "  <br>JD_WSCK用户 " + remarks + "   " + pin + " 已上线");
            }
            else
            {
                QLId = env["_id"].ToString();
                var upresult = await config.UpdateEnv(wskey, QLId, "JD_WSCK", remarks);
                timestamp = upresult.data["timestamp"].ToString();
                await _mainConfig.pushPlusNotify(@" 服务器;" + config.QLkey + " " + config.QLName + "  <br>JD_WSCK用户 " + remarks + "   " + pin + " 已更新 CK");
            }
            await config.Enable(QLId);
            result.success = true;
            result.data = new { qlid = QLId, nickname = Nickname, timestamp = timestamp, remarks = Nickname, qlkey = config.QLkey};
            return Ok(result);
        }

        [HttpPost, Route("VerifyCaptcha")]
        public async Task<IActionResult> VerifyCaptcha(ReqSliderCaptcha obj)
        {
            string Phone = obj.Phone;
            List<SliderCaptchaData> Pointlist = obj.point;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            if (string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
            if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
            try
            {
                result = await _PageServer.VerifyCaptcha( Phone, Pointlist);
            }
            catch (Exception e)
            {
                if (!string.IsNullOrEmpty(Phone))
                {
                    await _PageServer.PageClose(Phone); ;
                }
                result.data = new { Status = 404 };
                result.message = e.Message;
                result.success = false;
            }

            return Ok(result);
        }
        [HttpPost, Route("VerifyCode")]
        public async Task<IActionResult> VerifyCode(RequestEntity obj)
        {

            string Phone = obj.Phone;
            int qlkey = obj.qlkey;
            string Code = obj.Code;
            string qq = obj.QQ;
            ResultModel<object> result = ResultModel<object>.Create(true, "");
            if(string.IsNullOrEmpty(Phone)) throw new Exception("请输入手机号码");
            if (!CheckPhoneIsAble(Phone)) throw new Exception("请输入正确的手机号码");
            if (string.IsNullOrEmpty(Code)) throw new Exception("请输入验证码");
            if (_mainConfig.UPTYPE == UpTypeEum.ql && qlkey ==0) throw new Exception("请选择服务器");
            if (_mainConfig.UPTYPE == UpTypeEum.xdd && string.IsNullOrEmpty(qq)) throw new Exception("输入QQ号");
            try
            {
                 result = await _PageServer.VerifyCode(qlkey,qq,Phone, Code);
               
            }catch( Exception e)
            {
                if (!string.IsNullOrEmpty(Phone))
                {
                   await _PageServer.PageClose(Phone);;
                }
                result.data = new { Status = 404 };
                result.message = e.Message;
                result.success = false;
            }

            return Ok(result);
        }
        public  bool CheckPhoneIsAble(string input)
        {
            if (input.Length < 11)
            {
                return false;
            }
            Regex regex = new Regex("^1\\d{10}$");
            return regex.IsMatch(input);
        }
       
    }
}
