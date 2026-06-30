using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using TGPVD310SDK;

public static class PalmScanner
{
    private static byte[] g_pucFeature;
    private static byte[] g_pucTmpl;
    private static byte[] g_pucImage;

    private static int g_iFeatureSize = -1;
    private static int g_iTmplSize = -1;
    private static int g_iImageSize = -1;
    private static int g_iRegTimes = -1;

    private static SDPVD310API.MyDeg ShowMsgCallBack;
    private static SDPVD310API.ExtractFeatureCallback efCallBack;
    private static SDPVD310API.RegisterCallback regCallBack;

    private static bool isInitialized = false;
    private static bool isDeviceOpen = false;
    private static int? uid = null;

    private static readonly string url = "https://asti-my-palm-scan.odoo.com/jsonrpc";
    private static readonly string db = "asti-my-palm-scan-main-21933315";
    private static readonly string username = "admin";
    private static readonly string password = "889d4c53b8718cbb807467fcd608e776db4f9059";

    public static string Init()
    {
        ShowMsgCallBack = new SDPVD310API.MyDeg(ShowMsg);
        efCallBack = new SDPVD310API.ExtractFeatureCallback(ExtractFeatureCallbackMsg);
        regCallBack = new SDPVD310API.RegisterCallback(RegisterCallbackMsg);

        SDPVD310API.SD_API_GetBufferSize(ref g_iFeatureSize, ref g_iTmplSize, ref g_iImageSize, ref g_iRegTimes);

        g_pucImage = new byte[g_iImageSize];
        g_pucTmpl = new byte[g_iTmplSize];
        g_pucFeature = new byte[g_iFeatureSize];

        int ret = SDPVD310API.SD_API_Init(
            ShowMsgCallBack,
            "license.dat",
            EnApiAutoUpdTmpl.API_AUTOUPD_ON,
            EnApiLog.API_LOG_ON
        );

        if (ret != 0)
            return $"SDK Init Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        isInitialized = true;
        return "SDK initialized successfully.";
    }

    public static string OpenDevice()
    {
        if (!isInitialized)
            return "SDK not initialized.";

        byte[] fw = new byte[17];
        byte[] sn = new byte[17];

        int ret = SDPVD310API.SD_API_OpenDev(fw, sn);
        if (ret != 0)
            return $"Open Device Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        isDeviceOpen = true;
        return $"Device Opened: FW={Encoding.Default.GetString(fw)}, SN={Encoding.Default.GetString(sn)}";
    }

    public static string CloseDevice()
    {
        if (isDeviceOpen)
        {
            SDPVD310API.SD_API_CloseDev();
            isDeviceOpen = false;
        }

        if (isInitialized)
        {
            SDPVD310API.SD_API_Uninit();
            isInitialized = false;
        }

        return "Device and SDK closed.";
    }

    public static async Task<string> InitOdooAsync()
    {
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "common",
                method = "authenticate",
                args = new object[] { db, username, password, new { } }
            },
            id = 999
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetInt32() > 0)
        {
            uid = resultElem.GetInt32();
            return $"Odoo connected successfully. UID = {uid}";
        }
        else
        {
            uid = null;
            return $"Failed to authenticate with Odoo.\nRaw response: {json}";
        }
    }

    public static async Task<string> Register(string palmId, int contactId)
    {
        byte[] pucImages = new byte[g_iImageSize * g_iRegTimes];
        int ret = SDPVD310API.SD_API_Register(g_pucTmpl, pucImages, 0, regCallBack, 15);

        if (ret != 0)
            return $"Registration Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        string base64Tmpl = Convert.ToBase64String(g_pucTmpl);
        await UpdatePalmTemplateInContact(contactId, base64Tmpl);

        return $"Palm registered successfully for Palm ID: {palmId}, and saved in Odoo.";
    }

    public static async Task<string> Match()
    {
        if (uid == null)
        {
            var loginResult = await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        Console.WriteLine("Please place your hand on the scanner...");

        int ret = SDPVD310API.SD_API_ExtractFeature(g_pucFeature, g_pucImage, 0, efCallBack, 10);
        if (ret != 0)
            return $"Feature Extraction Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        var palmIdList = await GetAllPalmIdsFromContacts();
        foreach (var entry in palmIdList)
        {
            var palmId = entry.Key;
            var contact = entry.Value;
            byte[]? tmpl = await GetPalmTemplateFromContact(palmId);
            if (tmpl == null || tmpl.Length == 0) continue;

            ret = SDPVD310API.SD_API_Match1V1(g_pucFeature, tmpl);
            if (ret == 0)
            {
                return $"Match Success: Name = {contact["name"]}, Mobile = {contact["mobile"]}, Email = {contact["email"]}";
            }
        }

        return "Match Failed: No matching template found in Odoo.";
    }

    public static async Task<Dictionary<string, Dictionary<string, string>>> GetAllPalmIdsFromContacts()
    {
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "object",
                method = "execute_kw",
                args = new object[]
                {
                    db, uid, password,
                    "res.partner", "search_read",
                    new object[]
                    {
                        new object[] { new object[] { "x_studio_palm_id", "!=", false } }
                    },
                    new Dictionary<string, object>
                    {
                        { "fields", new[] { "x_studio_palm_id", "x_studio_palm_template", "name", "mobile", "email" } }
                    }
                }
            },
            id = 11
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var result = new Dictionary<string, Dictionary<string, string>>();
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem))
        {
            foreach (var item in resultElem.EnumerateArray())
            {
                string palmId = item.GetProperty("x_studio_palm_id").GetString();
                var contactInfo = new Dictionary<string, string>
                {
                    { "name", item.GetProperty("name").GetString() ?? "" },
                    { "mobile", item.GetProperty("mobile").GetString() ?? "" },
                    { "email", item.GetProperty("email").GetString() ?? "" }
                };
                result[palmId] = contactInfo;
            }
        }

        return result;
    }

    public static async Task<string> UpdatePalmTemplateInContact(int contactId, string base64Tmpl)
    {
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "object",
                method = "execute_kw",
                args = new object[]
                {
                    db, uid, password,
                    "res.partner", "write",
                    new object[]
                    {
                        new int[] { contactId },
                        new Dictionary<string, object> { { "x_studio_palm_template", base64Tmpl } }
                    }
                }
            },
            id = 6
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<byte[]?> GetPalmTemplateFromContact(string palmId)
    {
        var payload = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                service = "object",
                method = "execute_kw",
                args = new object[]
                {
                    db, uid, password,
                    "res.partner", "search_read",
                    new object[]
                    {
                        new object[] { new object[] { "x_studio_palm_id", "=", palmId.Trim() } }
                    },
                    new Dictionary<string, object>
                    {
                        { "fields", new[] { "x_studio_palm_template" } },
                        { "limit", 1 }
                    }
                }
            },
            id = 10
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetArrayLength() > 0)
        {
            var tmplBase64 = resultElem[0].GetProperty("x_studio_palm_template").GetString();
            if (!string.IsNullOrWhiteSpace(tmplBase64))
                return Convert.FromBase64String(tmplBase64);
        }

        return null;
    }

    private static void ShowMsg(IntPtr error)
    {
        string strBuffer = Marshal.PtrToStringUni(error);
        Console.WriteLine(strBuffer);
    }

    private static void ExtractFeatureCallbackMsg(int error, byte[] image, int imageSize, int[] image_roi_rect)
    {
        if (error != 0)
            Console.WriteLine(SDPVD310API.SD_API_GetErrMsg(error));
    }

    private static void RegisterCallbackMsg(int error, int stage, byte[] image, int imageSize, int[] image_roi_rect)
    {
        if (error != 0)
            Console.WriteLine(SDPVD310API.SD_API_GetErrMsg(error));
        else
            Console.WriteLine($"Registration progress: {stage}/10");
    }
}
