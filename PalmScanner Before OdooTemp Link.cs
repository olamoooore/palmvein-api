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

    public static async Task<string> TestOdooConnection()
    {
        return await InitOdooAsync();
    }

    public static async Task<string> Register(string userId)
    {
        if (uid == null)
        {
            var loginResult = await InitOdooAsync();
            if (uid == null) return "Odoo authentication failed.";
        }

        byte[] pucImages = new byte[g_iImageSize * g_iRegTimes];
        int ret = SDPVD310API.SD_API_Register(g_pucTmpl, pucImages, 0, regCallBack, 15);

        if (ret != 0)
            return $"Registration Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        int groupId = 0;
        int addRet = SDPVD310API.SD_API_AddTmpl(g_pucTmpl, groupId, userId);
        if (addRet != 0)
            return $"Add Template Failed: {SDPVD310API.SD_API_GetErrMsg(addRet)}";

        var employeeId = await GetEmployeeIdByMobile(userId);
        string odooResponse = "";
        if (employeeId != null)
        {
            odooResponse = await UpdatePalmIdInOdoo(employeeId.Value, userId);
        }
        else
        {
            var newId = await CreateEmployeeInOdoo(userId);
            if (newId != null)
            {
                odooResponse = $"No employee found. Created new employee with ID: {newId}.";
            }
            else
            {
                odooResponse = "No employee found and failed to create a new one in Odoo.";
            }
        }

        return $"User {userId} registered, template saved, added to memory, and pushed to Odoo.\nOdoo response: {odooResponse}";
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

        Console.WriteLine("Feature extracted. Now attempting to match...");

        byte[] ID = new byte[33];
        byte[] palmUpdTmpl = new byte[g_iTmplSize];
        int groupId = 0;

        ret = SDPVD310API.SD_API_Match1VNEx(g_pucFeature, groupId, ID, palmUpdTmpl);

        if (ret != 0)
            return $"Match Failed: {SDPVD310API.SD_API_GetErrMsg(ret)}";

        string matchedPalmId = Encoding.Default.GetString(ID).TrimEnd('\0');
        Console.WriteLine($"Match Success. Identified: {matchedPalmId}");

        var empInfo = await GetEmployeeInfoByPalmId(matchedPalmId);

        if (empInfo != null)
        {
            return $"Match Success: {empInfo}";
        }
        else
        {
            return $"Match Success, but no employee found in Odoo with Palm ID: {matchedPalmId}";
        }
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

    public static async Task<int?> GetEmployeeIdByMobile(string mobile)
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
                    "hr.employee", "search",
                    new object[]
                    {
                        new object[] { new object[] { "mobile_phone", "=", mobile.Trim() } }
                    }
                }
            },
            id = 1
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetArrayLength() > 0)
            return resultElem[0].GetInt32();

        return null;
    }

    public static async Task<string> UpdatePalmIdInOdoo(int employeeId, string palmId)
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
                    "hr.employee", "write",
                    new object[]
                    {
                        new int[] { employeeId },
                        new Dictionary<string, object> { { "x_studio_palm_id_1", palmId.Trim() } }
                    }
                }
            },
            id = 2
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    public static async Task<int?> CreateEmployeeInOdoo(string mobile)
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
                    "hr.employee", "create",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "name", $"Palm User {mobile}" },
                            { "mobile_phone", mobile },
                            { "x_studio_palm_id_1", mobile }
                        }
                    }
                }
            },
            id = 4
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem))
            return resultElem.GetInt32();

        return null;
    }

    public static async Task<string?> GetEmployeeInfoByPalmId(string palmId)
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
                    "hr.employee", "search_read",
                    new object[]
                    {
                        new object[] { new object[] { "x_studio_palm_id_1", "=", palmId.Trim() } }
                    },
                    new Dictionary<string, object>
                    {
                        { "fields", new[] { "name", "work_email", "mobile_phone" } },
                        { "limit", 1 }
                    }
                }
            },
            id = 3
        };

        using var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("result", out var resultElem) && resultElem.GetArrayLength() > 0)
        {
            var obj = resultElem[0];
            string name = obj.GetProperty("name").GetString() ?? "N/A";
            string email = obj.GetProperty("work_email").GetString() ?? "N/A";
            string mobile = obj.GetProperty("mobile_phone").GetString() ?? "N/A";
            return $"Name: {name}, Email: {email}, Mobile: {mobile}";
        }

        return null;
    }
}
