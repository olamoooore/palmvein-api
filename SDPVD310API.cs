using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
namespace TGPVD310SDK
{
    public enum EnApiAutoUpdTmpl
    {
        API_AUTOUPD_OFF,//关闭自动更新
        API_AUTOUPD_ON  //开启自动更新 
    }

    public enum EnApiLog
    {
        API_LOG_OFF, //关闭日志
        API_LOG_ON   //打开日志
    }

    //错误码id
    public enum SD_API_E_ERROR
    {
        SD_API_E_ERR_NOERROR,                                                   //无错误
        SD_API_E_ERR_INVALID_LICENSE = ((0x8 << 24) + 1),                       //许可证无效
        SD_API_E_ERR_NO_COM_API,                                                //没有通信库
        SD_API_E_ERR_NO_PROCESS_API,                                            //没有算法库
        SD_API_E_ERR_OTHERS,                                                    //未知错误
        SD_API_E_ERR_PARAMS_ERR,                                                //参数错误
        SD_API_E_ERR_MEMORY_INSUFFICIENT,                                       //内存不足
        SD_API_E_ERR_DEV_NOT_CONNECT,                                           //设备未连接
        SD_API_E_ERR_DEV_UNSUPPORT,                                             //设备不支持该功能
        SD_API_E_ERR_EXTRACT_FAILED,                                            //特征提取失败
        SD_API_E_ERR_GETTMPL_FAILED,                                            //特征融合失败
        SD_API_E_ERR_VERIFY_FAILED,                                             //验证失败
        SD_API_E_ERR_DELTEMPLATE_FAILED,                                        //删除失败
        SD_API_E_ERR_UPDATETEMPLATE_FAILED,                                     //更新失败
        SD_API_E_ERR_ADDTETEMPLATE_FAILED,                                      //添加失败
        SD_API_E_ERR_TIMEOUT,                                                   //超时
        SD_API_E_ERR_CANCEL,                                                    //取消采集
        SD_API_E_ERR_DEVBUSY,                                                   //设备正忙,请取消当前提取特征/注册任务!
        SD_API_E_ERR_PUTPALM,                                                   //请将手掌放到设备中心15cm-30cm左右的距离处!
        SD_API_E_ERR_AGAINPUTPALM,                                              //手掌检测中，请勿拿开手掌!
        SD_API_E_ERR_POSITION_NOT_FOUND,                                        //未检测到手掌
        SD_API_E_ERR_NO_LIVE_API,                                               //没有活体库
        SD_API_E_ERR_PALM_POSITION_NOT_CENTRE,                                  //手掌位置放置未居中
        SD_API_E_ERR_PALM_POSITION_TOO_LOW,                                     //手掌位置放置太低
        SD_API_E_ERR_PALM_POSITION_TOO_HIGHT,                                   //手掌位置放置太高
        SD_API_E_ERR_FINGERS_TOO_CLOSE,                                         //手指未自然张开
        SD_API_E_ERR_IMAGE_QUALITY_POOR,                                        //图像质量差
        SD_API_E_ERR_FEATURE_TOO_SIMILAR,                                       //特征相似度太高
        SD_API_E_ERR_FEATURE_TOO_DISSIMILAR,                                    //特征相似度太低
        SD_API_E_ERR_COMMUNICATION_FAILED,                                      //通信失败
        SD_API_E_ERR_TMPL_INVALID,                                              //模板无效
        SD_API_E_ERR_POSITION_INSTABILITY,                                      //手掌未放置稳定
        SD_API_E_ERR_NO_PROCESS_AI,                                             //没有算法ai库
        SD_API_E_ERR_NO_PROCESS_AI_FAILED,                                      //算法ai初始化失败
        SD_API_E_ERR_GRAY_TOO_HIGH,                                             //图像过曝
        SD_API_E_ERR_GRAY_TOO_LOW,                                              //图像太暗
        SD_API_E_ERR_NO_DEPTHPROCESS_API,                                       //没有深度算法库
        SD_API_E_ERR_DEPTHPROCESS_INIT_FAILED,                                  //深度算法初始化失败
        SD_API_E_ERR_LICENSE_NOT_FOUND,                                         //许可证未找到
        SD_API_E_ERR_GROUPID_NOT_FOUND,                                         //未找到该模板
        SD_API_E_ERR_GROUPID_NOT_TMPL,                                          //该组模板为空
        SD_API_E_ERR_TMPL_REPEAT,                                               //重复注册
        SD_API_E_ERR_PALMINFO_DATA_ERR,                                         //手掌信息错误
        SD_API_E_ERR_BUTT
    };

    
    public class SDPVD310API
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        /******************************************************************************
        Function:
            消息回调函数
        Input:
            error  错误码id，详见 SD_API_E_ERROR(错误码表)
        Output:
            无
        Return:
            void
        Others:
            无
        *****************************************************************************/
        public delegate void MyDeg(IntPtr error);
        /******************************************************************************
         Function:
             提取特征回调函数
         Parameter:
             error 错误码id，详见 SD_API_E_ERROR
             image bmp图像数据
             image bmp图像数据大小
             image_roi_rect 有效手掌区域的四个顶点位置坐标,从左上角开始顺时针方向排列(x1,y1),(x2,y2),(x3,y3),(x4,y4)，对应为
                 (image_roi_rect[0],image_roi_rect[1])
                 (image_roi_rect[2],image_roi_rect[3])
                 (image_roi_rect[4],image_roi_rect[5])
                 (image_roi_rect[6],image_roi_rect[7])
         Others:
             该回调函数配合提取特征使用
         *****************************************************************************/
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ExtractFeatureCallback(int error, [MarshalAs(UnmanagedType.LPArray, SizeConst = 257078)] byte[] image, int imageSize, [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] int[] image_roi_rect);

        /******************************************************************************
        Function:
            注册模板回调函数；
        Parameter:
            error 错误码id，详见 SD_API_E_ERROR
            stage 注册阶段
            image bmp图像数据
            image bmp图像数据大小
            image_roi_rect 有效手掌区域的四个顶点位置坐标,从左上角开始顺时针方向排列(x1,y1),(x2,y2),(x3,y3),(x4,y4)，对应为
                (image_roi_rect[0],image_roi_rect[1])
                (image_roi_rect[2],image_roi_rect[3])
                (image_roi_rect[4],image_roi_rect[5])
                (image_roi_rect[6],image_roi_rect[7])
        Others:
            无
        *****************************************************************************/
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RegisterCallback(int error, int stage, [MarshalAs(UnmanagedType.LPArray, SizeConst = 257078)] byte[] image, int imageSize, [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)]int[] image_roi_rect);


        /******************************************************************************
        Function:
            获取错误码id相应的错误信息(公用接口)
        Input:
            enError     错误码，参考SD_API_E_ERROR
        Output:
            无
        Return:
            SD_CONST_STRING          错误信息
        Others:
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetErrMsg", CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr SD_GetErrMsg(int enError);
        static public string SD_API_GetErrMsg(int enError)
        {
            IntPtr pErrMsg = SD_GetErrMsg(enError);
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(Marshal.PtrToStringUni(pErrMsg));
            string strBuffer = System.Text.Encoding.UTF8.GetString(bytes);
            strBuffer = new string((from c in strBuffer.ToCharArray() where !char.IsControl(c) select c).ToArray());
            return strBuffer;
        }

        /******************************************************************************
        Function:
            打开深度算法(公用接口)
        Input:
            无
        Output:
            无
        Return:
            SD_VOID
        Others:
             该接口在程序启动时，只能调用一次，并且在获取缓存接口和初始化接口之前.
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_DEPTH_ON", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_DEPTH_ON();
        /******************************************************************************
        Function:
            关闭深度算法(公用接口)
        Input:
            无
        Output:
            无
        Return:
            SD_VOID
        Others:
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_DEPTH_OFF", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_DEPTH_OFF();

        /******************************************************************************
        Function:
            设置注册次数
        Input:
            无
        Output:
            无
        Return:
            详见 SD_API_E_ERROR；
        Others:
            1：SDK内部默认是注册10次,如果要改为4次,调用次接口改为4次就可以
            2：该接口程序启动时调用一次，在初始化和获取缓存接口之前，之后不能再进行设置
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Reg_Times", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_Reg_Times(int times);

        /******************************************************************************
        Function:
            获取缓存大小(公用接口)
        Input:
            无
        Output:
             piFeatureSize 特征大小
             piTmplSize 模板大小
             piImageSize 原始图像大小
             piRegTimes 注册次数
        Return:
            void
        Others:
            程序启动时调用一次即可,不要重复调用!!!
    *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetBufferSize", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_GetBufferSize(
            ref int piFeatureSize,
            ref int piTmplSize,
            ref int piImageSize,
            ref int piRegTimes);

        /******************************************************************************
        Function:
            sdk 初始化(公用接口)
        Input:
            pushMsg 回调函数
            licensePath 授权license路径，包括文件名。
            enAutoUpdateTmpl 自动更新模板开关, 详见EnApiAutoUpdTmpl
                enAutoUpdateTmpl=API_AUTOUPD_ON:开启自动更新,执行SD_API_Match1VNEx比对时，将旧模板更新
                enAutoUpdateTmpl=API_AUTOUPD_OFF:关闭自动更新, 同理，关闭
            enWriteLog SDK日志开关, 详见EnApiLog
                enWriteLog=API_LOG_ON:打开日志，调用API接口,内部分析得出的一些错误信息，写入到文件中logs/SDAPI.log          
                enWriteLog=API_LOG_OFF:关闭日志，同理，不写入文件中logs/SDAPI.log 
        Output:
            无
        Return:
            详见 SD_API_E_ERROR；
        Others:
            1：初始化成功,才能调用以下所有接口
            2：初始化通信库，算法库，检查许可证等一系列操作
            3：程序启动时调用一次即可,不要重复调用!!!
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Init", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_Init(MyDeg pushMsg,string licensePath,EnApiAutoUpdTmpl enAutoUpdateTmpl,EnApiLog enWriteLog);
        /******************************************************************************
         Function:
             sdk 去初始化(公用接口)
         Input:
             无
         Output:
             无
         Return:
             void
         Others:
             1：关闭设备,卸载通信库及算法库，释放内存空间等
             2：程序结束时调用一次即可,不要重复调用!!!
         *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Uninit", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_Uninit();


        /******************************************************************************
        Function:
            打开设备(设备端接口)
        Input:
            无
        Output:
            fw:获取到的固件号(16 Bytes),字符串大小设置为16+1字节
            sn:获取到的序列号(16 Bytes),字符串大小设置为16+1字节
        Return:
            详见 SD_API_E_ERROR
        Others:
            如果当前处于注册/提取特征任务时，请调用接口SD_API_Cancel取消任务,再打开设备
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_OpenDev", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_OpenDev(byte[] fw, byte[] sn);

        /******************************************************************************
        Function:
            关闭设备(设备端接口)
        Input:
            无
        Output:
            无
        Return:
            详见 SD_API_E_ERROR
        Others:
            如果当前处于注册/提取特征任务时，请调用接口SD_API_Cancel取消任务,再关闭设备
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_CloseDev", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_CloseDev();

        /******************************************************************************
        Function:
            获取设备状态(设备端接口)
        Input:
            无
        Output:
            无
        Return:
            SD_API_E_ERR_DEV_NOT_CONNECT:设备未连接
            API_ERR_NOERROR:设备已连接
            其他错误详见 SD_API_E_ERROR
        Others:
            无
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetDevStatus", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_GetDevStatus();

        /******************************************************************************
       Function:
           取消采集(设备端接口)
       Input:
           无
       Output:
           无
       Return:
           无
       Others:
           如果当前正在采集图像或者正在提取特征或者正在注册模板，调用此api接口，可以取消之
       *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Cancel", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_Cancel();


        /******************************************************************************
        Function:
            获取特征压缩掌信息(设备端)
         Input:
             sn  设备序列号，1：填NULL，默认打开一个设备;2:填写17位sn号，指定设备打开;
             callback 回调函数，可为NULL
             timeOut:超时时间，单位为秒。-1：表示不限时
         Output:
             cmpPalmInfo 压缩掌信息(如果获取成功,SDK内部申请空间，外部需要释放)
             cmpPalmInfoSize 压缩掌信息大小
         Return:
             详见 SD_API_E_ERROR
         Others:
        获取压缩掌信息成功时，可以调用SD_API_GetFeature_CmpPalmInfo接口获取到特征数据
        *****************************************************************************/
        [DllImport(@"SDPVD310API.dll", EntryPoint = "SD_API_GetFtCmpPalmInfo", CallingConvention = CallingConvention.Cdecl)]
        private extern static int SD_GetFtCmpPalmInfo(string sn,
                        ref IntPtr cmpPalmInfo,
                        ref int cmpPalmInfoSize,
                        ExtractFeatureCallback callback,
                        int timeOut);
        static public int SD_API_GetFtCmpPalmInfo(string sn,
                         ref byte[] cmpPalmInfo,
                         ExtractFeatureCallback callback,
                         int timeOut)
        {
            int cmpPalmInfoSize = 0;
            IntPtr ptr = IntPtr.Zero;//指针内存空间由sdk内部申请，外部释放
            int errid = SD_GetFtCmpPalmInfo(sn,ref ptr, ref cmpPalmInfoSize, callback, timeOut);
            if (errid == 0)
            {
                cmpPalmInfo = new byte[cmpPalmInfoSize];
                Marshal.Copy(ptr, cmpPalmInfo, 0, cmpPalmInfoSize);
                Marshal.FreeHGlobal(ptr);//释放内存
                ptr = IntPtr.Zero;//指向为空
            }
            return errid;
        }


        /******************************************************************************
        Function:
            获取注册压缩掌信息(设备端)
        Input:
            sn  设备序列号，1：填NULL，默认打开一个设备;2:填写17位sn号，指定设备打开;
            timeOut:超时时间，单位为秒。-1：表示不限时
            callback 透传给RegisterCallback回调函数的参数，供回调函数使用，可为NULL
        Output:
            cmpPalmInfo 压缩掌信息(如果获取成功，内部进行内存空间申请，外部释放)
            cmpPalmInfoSize 压缩掌信息大小
        Return:
            详见 SD_API_E_ERROR
        Others:
            获取压缩掌信息成功时，可以调用SD_API_GetTmpl_FromPalmInfo接口获取到模板数据
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetRegCmpPalmInfo", CallingConvention = CallingConvention.Cdecl)]
        private extern static int SD_GetRegCmpPalmInfo(string sn, 
                          ref IntPtr cmpPalmInfo,
                          ref int cmpPalmInfoSize,
                          RegisterCallback callback,
                          int timeOut);
        static public int SD_API_GetRegCmpPalmInfo(string sn,
                         ref byte[] cmpPalmInfo,
                         RegisterCallback callback,
                         int timeOut)
        {
            int cmpPalmInfoSize = 0;
            IntPtr ptr = IntPtr.Zero;//指针内存空间由sdk内部申请，外部释放
            int errid = SD_GetRegCmpPalmInfo(sn,ref ptr, ref cmpPalmInfoSize, callback, timeOut);
            if (errid == 0)
            {
                cmpPalmInfo = new byte[cmpPalmInfoSize];
                Marshal.Copy(ptr, cmpPalmInfo, 0, cmpPalmInfoSize);
                Marshal.FreeHGlobal(ptr);//释放内存
                ptr = IntPtr.Zero;//指向为空
            }
            return errid;
        }

        /******************************************************************************
        Function:
            从注册压缩掌信息中获取模板(服务端)
        Input:
            cmpPalmInfo 压缩掌信息
            cmpPalmInfoSize 压缩掌信息大小
            iGroupId 分组ID(默认分组ID为-1),用来验证该模版时否重复
        Output:
            pucTmpl 掌静脉模板(内存空间不小于掌静脉模板大小)
        Return:
            详见 SD_API_E_ERROR
        Others:
            1：调用SD_API_Register_CmpPalmInfo接口获取注册压缩掌信息
            2：将注册压缩掌信息作为参数传入该接口，获取模板。
            3: 该接口会进行模板查重功能，前提需要调用SD_API_AddTmpl接口将模板添加sdk内部中。
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetTmpl_FromRegCmpPalmInfo", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_GetTmpl_FromRegCmpPalmInfo(
               byte[] cmpImgData,
               int cmpPalmInfoSize,
               int iGroupId,
               byte[] pucTmpl);

        /******************************************************************************
        Function:
            从特征压缩掌信息中获取掌静脉特征(服务端)
        Input:
            cmpPalmInfo 压缩掌信息
            cmpPalmInfoSize 压缩掌信息大小
        Output:
            pucFeature 掌静脉特征(内存空间不小于特征大小)
        Return:
            详见 SD_API_E_ERROR
        Others:
            1：调用SD_API_GetFtCmpPalmInfo接口获取特征压缩掌信息
            2：将特征压缩掌信息作为参数传入该接口，获取特征
            *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetFeature_FromFtCmpPalmInfo", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_GetFeature_FromFtCmpPalmInfo(
           byte[] cmpPalmInfo,
           int cmpPalmInfoSize,
           byte[] pucFeature);

        /******************************************************************************
        Function:
            添加模板(服务端)
        Input:
            pucTmpl 待添加模板
            ID  手掌ID(33个字节)
        Return:
            详见 SD_API_E_ERROR
        Others:
            返回值为SD_API_E_ERR_NOERROR时为成功。
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_AddTmpl", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_AddTmpl(byte[] pucTmpl,int iGroupId,string ID);
        /******************************************************************************
        Function:
            删除模板(服务端)
        Input:
            iGroupId 分组ID(默认分组ID为-1)
            ID 手掌ID(33个字节)
        Return:
            详见 SDKEnum.SD_API_E_ERROR
        Others:
            返回值为SD_API_E_ERR_NOERROR时为成功。
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_DelTmpls", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_DelTmpls(string ID);
        /******************************************************************************
        Function:
            清空模板(服务端)
        Return:
            void
        Others:无
         *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_ClearTmpls", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SD_API_ClearTmpls();


        /******************************************************************************
        Function:
            掌静脉比对(服务端)
        Input:
            pucFeature 待比对的掌静脉特征，该数据通过提取特征获取
            pucTmpl 待比对的掌静脉模板，该数据通过注册模板获取
            iTmplNums 掌静脉的模板数,
                  如果pucTmpl申请的是1个模板,pucTmpl申请的内存为模板大小*1,iTmplNums=1
                  如果pucTmpl申请的是n个模板,pucTmpl申请的内存为模板大小*n,iTmplNums=n
        Output:
            piMatchIdx 在比对中，与之相匹配的"模板"的位置(从0开始)
            pucUpdTmpl 更新模板，因为掌静脉是不断变化的，需要将新模板替换旧模板，提高比对准确率
        Return:
            详见 SD_API_E_ERROR
        Others:
            传入的模版需要开发者将所有模版拼接好传入该接口(pucTmpl=pucTmpl_1+pucTmpl_2+........)
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Match1VN", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_Match1VN(byte[] pucFeature, byte[] pucTmpl, int iTmplNums, ref int piMatchIdx, byte[] pucUpdTmpl);


        /******************************************************************************
        Function:
            SDK手掌比对(服务端)
        Input:
            pucFeature 待比对静脉特征
            iGroupId 分组ID
        Output:
            ID 在特征比对中，与之相匹配的用户ID(内存空间大小>=32位)
            pucUpdTmpl 更新模板
        Return:
            详见 SD_API_E_ERROR
        Others:
            调用该接口时，需要将模板通过SD_API_AddTmpl将模板保存在sdk内部内存中，才能进行比对。
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Match1VNEx", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_Match1VNEx(byte[] pucFeature, int iGroupId, byte[] ID, byte[] pucUpdTmpl);

        /******************************************************************************
        Function:
            采集设备图像数据(旧版本-设备端)
        Input:
            trigger_timeout 输入，等待设备被触发的时长（-1~1000），单位为秒。其中，0：表示未触发时立即返回；-1：表示无限等待。中途取消参阅取消接口;
        Output:
            pucImage 掌静脉图像
        Return:
            详见 SD_API_E_ERROR
        Others:
            无
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetImage", CallingConvention = CallingConvention.Cdecl)]
        public extern static int  SD_API_GetImage(
            byte[] pucImage,
            int  trigger_timeout);

        /******************************************************************************
        Function:
            提取特征(旧版本-设备端)
        Input:
            masked  打码图片,回调函数中的图片是否打码
                    SD_TRUE:打码，SD_FALSE:不打码
            timeOut:超时时间，单位为秒。时间范围（-1~1000）,-1：表示不限时
            ExtractFeatureCallback 回调函数，显示掌静脉的实时图片数据
        Output:
            pucFeature 掌静脉特征(内存空间不小于特征大小)
            pucImage   掌静脉图片(内存空间不小于图片大小)
        Return:
            详见 SD_API_E_ERROR
        Others:
            使用前请确认已调用SD_API_GetBufferSize获取到了特征大小和图像大小
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_ExtractFeature", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_ExtractFeature(
            byte[] pucFeature,
            byte[] pucImage,
            int  masked,
            ExtractFeatureCallback callback,
            int timeOut);

        /******************************************************************************
        Function:
            注册模板(旧版本-设备端)
        Input:
            timeOut:超时时间，单位为秒。时间范围（-1~1000）,其中，-1：表示不限时
            RegisterCallback 回调函数，显示掌静脉的实时图片数据和注册阶段等相关信息
            masked  打码图片,回调函数中的图片是否打码
                    SD_TRUE:打码，SD_FALSE:不打码
        Output:
            pucTmpl 手掌模板(内存空间不小于模板大小)，
            pucImages 手掌图像(内存空间不小于图像大小*采集次数)
        Return:
            详见 SD_API_E_ERROR；
        Others:
            1：使用前请确认已调用SD_API_GetBufferSize获取到了模板大小,图像大小及采集次数
            2：在规定时间采集4次正常手掌图像，融合成一个模板返回。
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_Register", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_Register(
            byte[] pucTmpl,
            byte[] pucImages,
            int masked,
            RegisterCallback callback,
            int timeOut);

        /******************************************************************************
        Function:
            从掌静脉图像中提取特征(旧版本-服务端)
        Input:
            pucImage 掌静脉图像
        Output:
            pucFeature 掌静脉特征(内存空间不小于特征大小)
        Return:
            详见 SD_API_E_ERROR；
        Others:
            1:使用前请确认已调用SD_API_GetBufferSize获取到了特征大小
            2:该图像必须是提取特征或者注册时获取到的图像数据
        *****************************************************************************/
        [DllImport(@".\SDPVD310API.dll", EntryPoint = "SD_API_GetFeatureFromImage", CallingConvention = CallingConvention.Cdecl)]
        public extern static int SD_API_GetFeatureFromImage(
            byte[] pucImage,
            byte[] pucFeature);

    }
}
