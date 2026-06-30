using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TGPVD310SDK;

public static class PalmScanner
{
    private static byte[] g_pucFeature;
    private static byte[] g_pucTmpl;
    private static byte[] g_pucImage;
    private static byte[] g_reg_cmpPalmInfo;
    private static byte[] g_feature_cmpPalmInfo;

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

        int ret = SDPVD310API.SD_API_Init(
            ShowMsgCallBack,
            "license.dat",
            EnApiAutoUpdTmpl.API_AUTOUPD_ON,
            EnApiLog.API_LOG_ON
        );

        if (ret != 0)
            return $"SDK Init Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        PrepareSDKBuffers();
        isInitialized = true;
        return "SDK initialized successfully.";
    }

    private static void PrepareSDKBuffers()
    {
        SDPVD310API.SD_API_GetBufferSize(ref g_iFeatureSize, ref g_iTmplSize, ref g_iImageSize, ref g_iRegTimes);
        g_pucImage = new byte[g_iImageSize];
        g_pucTmpl = new byte[g_iTmplSize];
        g_pucFeature = new byte[g_iFeatureSize];
        g_reg_cmpPalmInfo = new byte[g_iImageSize];
        g_feature_cmpPalmInfo = new byte[g_iImageSize];
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
        return $"Device Opened: FW={Encoding.Default.GetString(fw).Trim()}, SN={Encoding.Default.GetString(sn).Trim()}";
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

    public static async Task<string> TestOdooConnection()
    {
        return await InitOdooAsync();
    }

    public static async Task<string> Register(string mobile)
    {
        if (uid == null)
        {
            await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        byte[] pucImages = new byte[g_iImageSize * g_iRegTimes];
        int ret = SDPVD310API.SD_API_Register(g_pucTmpl, pucImages, 0, regCallBack, 15);

        if (ret != 0)
            return $"Registration Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        string base64Tmpl = Convert.ToBase64String(g_pucTmpl);
        var contactId = await GetContactIdByMobile(mobile);
        if (contactId == null)
            return $"No contact found in Odoo with Mobile: {mobile}";

        await UpdatePalmTemplateInContact(contactId.Value, base64Tmpl);
        return $"Palm registered successfully for Mobile: {mobile}, and saved in Odoo.";
    }

    public static async Task<string> Match()
    {
        if (uid == null)
        {
            await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        int ret = SDPVD310API.SD_API_ExtractFeature(g_pucFeature, g_pucImage, 0, efCallBack, 10);
        if (ret != 0)
            return $"Feature Extraction Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        Console.WriteLine("Clearing internal SDK templates...");
        SDPVD310API.SD_API_ClearTmpls();

        var contactList = await GetAllMobilePalmTemplates();
        Console.WriteLine($"Loaded {contactList.Count} contacts with palm templates");

        foreach (var entry in contactList)
        {
            string mobile = entry.Key;
            var contact = entry.Value;
            byte[]? tmpl = await GetPalmTemplateFromMobile(mobile);
            if (tmpl == null || tmpl.Length == 0)
            {
                Console.WriteLine($"❌ Skipping {mobile}: No template found");
                continue;
            }

            ret = SDPVD310API.SD_API_AddTmpl(tmpl, 1, mobile);
            if (ret != 0)
                Console.WriteLine($"⚠️ Failed to add template for {mobile}: {SDPVD310API.SD_API_GetErrMsg(ret)}");
            else
                Console.WriteLine($"✅ Added template for {mobile}");
        }

        byte[] matchedID = new byte[33];
        byte[] updatedTmpl = new byte[g_iTmplSize];
        ret = SDPVD310API.SD_API_Match1VNEx(g_pucFeature, 1, matchedID, updatedTmpl);

        if (ret == 0)
        {
            string id = Encoding.Default.GetString(matchedID).TrimEnd('\0').Trim();
            Console.WriteLine($"🎯 Match found for ID = {id}");

            if (contactList.ContainsKey(id))
            {
                var c = contactList[id];
                string storedBase64 = c["template"];
                string updatedBase64 = Convert.ToBase64String(updatedTmpl);

                if (storedBase64 != updatedBase64)
                {
                    Console.WriteLine("🛠 Template mismatch. Updating Odoo with new template...");

                    bool updateSuccess = await UpdatePalmTemplateInOdoo(id, updatedBase64);
                    string updateMsg = updateSuccess ? "✅ Template updated in Odoo." : "⚠️ Failed to update template in Odoo.";

                    return $"Match Success: Name = {c["name"]}, Mobile = {c["mobile"]}, Email = {c["email"]}\n{updateMsg}";
                }
                else
                {
                    return $"Match Success: Name = {c["name"]}, Mobile = {c["mobile"]}, Email = {c["email"]}\n🟢 Template already up to date.";
                }
            }

            return $"Match Success: Matched ID = {id}, but not found in contact dictionary.";
        }

        return "Match Failed: No matching template found in Odoo.";
    }

    private static async Task<bool> UpdatePalmTemplateInOdoo(string mobile, string newBase64)
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
                    new object[]
                    {
                        new object[] { "mobile", "=", mobile }
                    },
                    new Dictionary<string, object>
                    {
                        { "x_studio_palm_template", newBase64 }
                    }
                }
                }
            },
            id = 22
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetBoolean();
    }

    private static async Task<int?> GetContactIdByMobile(string mobile)
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
                    "res.partner", "search",
                    new object[]
                    {
                        new object[] { new object[] { "mobile", "=", mobile.Trim() } }
                    }
                }
            },
            id = 12
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetArrayLength() > 0)
        {
            return resultElem[0].GetInt32();
        }

        return null;
    }

    private static async Task<string> UpdatePalmTemplateInContact(int contactId, string base64Tmpl)
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

    private static async Task<Dictionary<string, Dictionary<string, string>>> GetAllMobilePalmTemplates()
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
                    new object[] { new object[] { "x_studio_palm_template", "!=", false } }
                },
                new Dictionary<string, object>
                {
                    { "fields", new[] { "x_studio_palm_template", "name", "mobile", "email" } }
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
                string mobile = item.GetProperty("mobile").GetString() ?? "";

                var contactInfo = new Dictionary<string, string>
            {
                { "name", item.GetProperty("name").GetString() ?? "" },
                { "mobile", mobile },
                { "email", item.GetProperty("email").GetString() ?? "" },
                { "template", item.GetProperty("x_studio_palm_template").GetString() ?? "" } // ✅ added
            };

                result[mobile] = contactInfo;
            }
        }

        return result;
    }


    private static async Task<byte[]?> GetPalmTemplateFromMobile(string mobile)
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
                        new object[] { new object[] { "mobile", "=", mobile.Trim() } }
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

    public static async Task<string> VerifyMobileTemplate(string mobile)
    {
        if (uid == null)
        {
            await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        byte[]? tmpl = await GetPalmTemplateFromMobile(mobile);
        if (tmpl != null && tmpl.Length > 0)
            return $"✅ Template exists for {mobile} (size = {tmpl.Length} bytes)";
        else
            return $"❌ No template found for {mobile}";
    }

    private static void RegisterCallbackMsg(int error, int stage, byte[] image, int imageSize, int[] image_roi_rect)
    {
        if (error != 0)
            Console.WriteLine(SDPVD310API.SD_API_GetErrMsg(error));
        else
            Console.WriteLine($"Registration progress: {stage}/10");
    }

    public static async Task<string> DeletePalmTemplate(string mobile)
    {
        if (uid == null)
        {
            await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        // Step 1: Get contact ID by mobile number
        var contactId = await GetContactIdByMobile(mobile);
        if (contactId == null)
            return $"❌ No contact found in Odoo with Mobile: {mobile}";

        // Step 2: Send update request to set palm template to false (i.e. delete it)
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
                    new int[] { contactId.Value },
                    new Dictionary<string, object>
                    {
                        { "x_studio_palm_template", false }
                    }
                }
                }
            },
            id = 26
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetBoolean())
        {
            return $"✅ Palm template deleted for mobile: {mobile}";
        }
        else
        {
            return $"⚠️ Failed to delete palm template for mobile: {mobile}. Raw: {json}";
        }
    }

} // end PalmScanner