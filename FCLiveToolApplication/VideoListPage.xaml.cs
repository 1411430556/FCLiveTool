using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using CommunityToolkit.Maui.Storage;
using Encoding = System.Text.Encoding;

#if ANDROID
using Android.Content;
using Java.Security;
using Android.Provider;
#endif

#if WINDOWS
using Windows.Storage.Pickers;
using Windows.Media.Protection.PlayReady;
#endif

namespace FCLiveToolApplication;

public partial class VideoListPage : ContentPage
{
    public VideoListPage()
    {
        InitializeComponent();
    }
    public static VideoListPage videoListPage;
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (videoListPage!=null)
        {
            return;
        }
        videoListPage=this;

        //����ҳ�Ĳ����б����õ�ǰҳ�Ĳ����б�
        //�������û����ܻ��ڵ����ϵ�ȡ����ť�����Բ�ʹ���������÷�����
        //VideoPrevPage.videoPrevPage.M3U8PlayList=M3U8PlayList;

        LoadVideos();
        InitRegexList();
        DeviceDisplay.MainDisplayInfoChanged+=DeviceDisplay_MainDisplayInfoChanged;
    }
    private void DeviceDisplay_MainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
    {
#if ANDROID

        //����
        if (DeviceDisplay.Current.MainDisplayInfo.Orientation==DisplayOrientation.Landscape)
        {
            EditModeRightPanel.Margin=new Thickness(0, 0, 0, 0);
        }
        else
        {
            EditModeRightPanel.Margin=new Thickness(0, 60, 0, 0);
        }
#endif
    }
    /// <summary>
    /// ��ʼ�������б�
    /// </summary>
    public void InitRegexList()
    {
        List<string> RegexOption = new List<string>() { "�Զ�ƥ��", "����1", "����2", "����3", "����4", "����5" };
        RegexSelectBox.ItemsSource = RegexOption;

        //RegexSelectBox.SelectedIndex = 0;
    }

    public int VLCurrentPageIndex = 1;
    public int VDLCurrentPageIndex = 1;
    public int VLMaxPageIndex;
    public int VDLMaxPageIndex;
    public const int VL_COUNT_PER_PAGE = 15;
    public const int VDL_COUNT_PER_PAGE = 100;
    public string AllVideoData;
    public List<VideoList> CurrentVideosList;
    public List<VideoDetailList> CurrentVideosDetailList;
    public string CurrentVURL = "";
    public string RecommendReg = "0";
    public bool IgnoreSelectionEvents = false;
    public List<string[]> M3U8PlayList = new List<string[]>();
    public bool isFinishM3U8VCheck = true;
    public int M3U8VCheckFinishCount = 0;
    public bool ShowLoadOrRefreshDialog = false;
    CancellationTokenSource M3U8ValidCheckCTS;
    public const string DEFAULT_USER_AGENT = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36";

    private void Question_Clicked(object sender, EventArgs e)
    {
        ImageButton button = sender as ImageButton;

        switch (button.StyleId)
        {
            case "VideoListQue":
#if ANDROID
                DisplayAlert("������Ϣ", "����ֱ��Դ�����������磬���ǲ���ֱ��������ݸ���������Ȩ����ϵ����ɾ��"+
                "\n��Ҫˢ���б��������б���ˢ�£�Ҳ���Ե������ť����ˢ�°�ťˢ�¡�", "�ر�");
#else
                DisplayAlert("������Ϣ", "����ֱ��Դ�����������磬���ǲ���ֱ��������ݸ���������Ȩ����ϵ����ɾ��", "�ر�");
#endif
                break;
            case "VideosQue":
#if ANDROID
                DisplayAlert("������Ϣ", "����ĳЩM3U8��URLʹ�õ�����Ե�ַ�����Ǿ��Ե�ַ����APP�޷����ţ�������APP��bug����֪Ϥ"+
                "\n��Ҫˢ���б��������б���ˢ�£�Ҳ���Ե������ť����ˢ�°�ťˢ�¡�", "�ر�");
#else
                DisplayAlert("������Ϣ", "����ĳЩM3U8��URLʹ�õ�����Ե�ַ�����Ǿ��Ե�ַ����APP�޷����ţ�������APP��bug����֪Ϥ", "�ر�");
#endif

                break;
        }
    }
    /// <summary>
    /// �����ã���������
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void DownloadBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoDetailList.SelectedItem==null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ�����ļ���", "ȷ��");
            return;
        }


        int statusCode;
        VideoDetailList selectVDL = VideoDetailList.SelectedItem as VideoDetailList;

        using (HttpClient httpClient = new HttpClient())
        {

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(DEFAULT_USER_AGENT);
            /*
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, selectVDL.SourceLink);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
           */

            HttpResponseMessage response = null;

            try
            {
                response = await httpClient.GetAsync(selectVDL.SourceLink);

                statusCode=(int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ����Ժ����ԣ�\n"+"HTTP������룺"+statusCode, "ȷ��");
                    return;
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "�޷����ӵ��Է�����������������������߸���һ��ֱ��Դ��", "ȷ��");
                return;
            }

            //var httpstream = await httpClient.GetStreamAsync(selectVDL.SourceLink)
            using (var httpstream = await response.Content.ReadAsStreamAsync())
            {
                try
                {
                    selectVDL.SourceName=selectVDL.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");
                    var fileSaver = await FileSaver.SaveAsync(FileSystem.AppDataDirectory, selectVDL.SourceName+".m3u8", httpstream, CancellationToken.None);

                    if (fileSaver.IsSuccessful)
                    {
                        await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n"+fileSaver.FilePath, "ȷ��");
                    }
                    else
                    {
                        //��ʱ�ж�Ϊ�û���ѡ��Ŀ¼ʱ�����ȡ����ť
                        await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
                    }
                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�ޡ�", "ȷ��");
                }
            }

        }



        /*
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("")))
         */

    }
    /*
             var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
{
    { DevicePlatform.iOS, new[] { "com.apple.mpegurl", "public.m3u8-playlist" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.macOS, new[] { "public.m3u8", "application/vnd.apple.mpegurl" } },
    { DevicePlatform.Android, new[] { "application/x-mpegURL" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.WinUI, new[] { ".m3u8" ,".m3u"} }
});

        var filePicker = await FilePicker.PickAsync(new PickOptions()
        {
            PickerTitle="",
            FileTypes=fileTypes
        });

        if (filePicker is not null)
        {
            await DisplayAlert("", filePicker.FullPath, "ȷ��");
        }

     */
    /// <summary>
    /// �����ã���������
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void EditBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoDetailList.SelectedItem==null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

        VideoDetailList selectVDL = VideoDetailList.SelectedItem as VideoDetailList;
        if (string.IsNullOrWhiteSpace(selectVDL.SourceName))
        {
            await DisplayAlert("��ʾ��Ϣ", "�޷��༭��ֱ��Դ���ⲻ�����ԭ����Ϊ�ڽ���ʱδ�ܽ�����ֱ��Դ���ơ�", "ȷ��");
            return;
        }
        string vname = selectVDL.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");

        string newvalue = await DisplayPromptAsync("�༭ֱ��Դ", "��ǰֱ��Դ���ƣ�"+vname+"\n"+"�������µ�ֱ��Դ���ƣ�", "����", "ȡ��", "Name", -1, null, vname);
        if (string.IsNullOrWhiteSpace(newvalue))
        {
            if (newvalue !=null)
                await DisplayAlert("��ʾ��Ϣ", "���Ʋ���Ϊ�ջ�հ��ַ���", "ȷ��");
            return;
        }

        /*
                 var tresult = GetOLDStr(AllVideoData, vname, selectVDL.SourceLink.Replace("\r", "").Replace("\n", "").Replace("\r\n", ""));
                if (tresult is null||tresult.Contains("#EXTINF"))
                {
                    await DisplayAlert("��ʾ��Ϣ", "�޷����µ�ǰֱ��Դ����ΪM3U�ļ��ڰ�������͵�ǰֱ��Դ��ͬ������+URL��", "ȷ��");
                    return;
                }




                string m3u8Str = GetFullM3U8Str(selectVDL);
        if (m3u8Str is "")
        {
            await DisplayAlert("��ʾ��Ϣ", "�޷����µ�ǰֱ��Դ����M3U�ļ����Ҳ�����ǰֱ��Դ��", "ȷ��");
            return;
        }

                string regexIndex = GetRegexOptionIndex();
        if (regexIndex=="0")
            regexIndex=RecommendReg;
        if (regexIndex.StartsWith("1")||regexIndex.StartsWith("2"))
        {
            vname="tvg-name=\""+vname+"\"";
            newvalue="tvg-name=\""+newvalue+"\"";
        }
        else
        {
            vname=","+vname;
            newvalue=","+newvalue;
        }
         */



        var tlist = CurrentVideosDetailList.Where(p => p==selectVDL).FirstOrDefault();
        if(tlist is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "δ�ڵ�ǰ�б��ڲ��ҵ���ѡ��ֱ��Դ����ˢ���б����ԡ�", "ȷ��");
            return;
        }
        tlist.SourceName=newvalue;

        if(tlist.FullM3U8Str.Contains("tvg-name="))
        {
            newvalue="tvg-name=\""+newvalue+"\"";
        }
        else
        {

        }


        //���ڵ��� AllVideoData�Ƿ���ȷ�޸� �Լ� AllVideoData�ܷ����������ص��б� ʱʹ��
        /*
                 string regexIndex = GetRegexOptionIndex();
                string treg;
                if (regexIndex!="0")
                {
                    treg=regexIndex;
                }
                else
                {
                    treg=RecommendReg;
                }

                List<VideoDetailList> tlist = DoRegex(AllVideoData, treg);
         */

        VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
        SetVDLPage(1);

        MakeVideosDataToPage(CurrentVideosDetailList, 0);

        await DisplayAlert("��ʾ��Ϣ", "���³ɹ���", "ȷ��");
    }
    /*
        public string CombieNewM3U8Str(string oldFullStr, string newvalue)
        {
            Match match = Regex.Match(oldFullStr, UseRegex(RecommendReg));

            if(RecommendReg=="")
            {
                return "";
            }

            return "";
        }
     */
    private async void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoDetailList.SelectedItem==null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

        VideoDetailList selectVDL = VideoDetailList.SelectedItem as VideoDetailList;
        if (string.IsNullOrWhiteSpace(selectVDL.SourceName))
        {
            await DisplayAlert("��ʾ��Ϣ", "�޷��༭��ֱ��Դ���ⲻ�����ԭ����Ϊ�ڽ���ʱδ�ܽ�����ֱ��Դ���ơ�", "ȷ��");
            return;
        }
        string vname = selectVDL.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");

        bool readydel = await DisplayAlert("��ȷ��Ҫ�Ƴ���ֱ��Դ��", vname, "ȷ��", "ȡ��");
        if (readydel)
        {
            /*
              var tresult = GetOLDStr(AllVideoData, vname, selectVDL.SourceLink.Replace("\r", "").Replace("\n", "").Replace("\r\n", ""));
             if (tresult is null||tresult.Contains("#EXTINF"))
             {
                 await DisplayAlert("��ʾ��Ϣ", "�޷��Ƴ���ǰֱ��Դ����ΪM3U�ļ��ڰ�������͵�ǰֱ��Դ��ͬ������+URL��", "ȷ��");
                 return;
             }



                        string m3u8Str = GetFullM3U8Str(selectVDL);
            if (m3u8Str is "")
            {
                await DisplayAlert("��ʾ��Ϣ", "�޷��Ƴ���ǰֱ��Դ����M3U�ļ����Ҳ�����ǰֱ��Դ��", "ȷ��");
                return;
            }
            if (m3u8Str is null)
            {
                AllVideoData=AllVideoData.Replace(selectVDL.FullM3U8Str, "");
            }
            else
            {
                AllVideoData=AllVideoData.Replace(m3u8Str, "");
            }
             */

            AllVideoData=AllVideoData.Replace(selectVDL.FullM3U8Str, "");
            CurrentVideosDetailList.Remove(selectVDL);

            //���ڵ��� AllVideoData�Ƿ���ȷ�޸� �Լ� AllVideoData�ܷ����������ص��б� ʱʹ��
            /*
                     string regexIndex = GetRegexOptionIndex();
                    string treg;
                    if (regexIndex!="0")
                    {
                        treg=regexIndex;
                    }
                    else
                    {
                        treg=RecommendReg;
                    }

                    List<VideoDetailList> tlist = DoRegex(AllVideoData, treg);
             */

            VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
            SetVDLPage(1);

            MakeVideosDataToPage(CurrentVideosDetailList, 0);

            await DisplayAlert("��ʾ��Ϣ", "�Ƴ��ɹ���", "ȷ��");

        }

    }

    /*
         public string GetFullM3U8Str(VideoDetailList vdlList)
    {
        //���������ʽ���ģ��������Ҳ��Ҫ����
        string regexIndex = GetRegexOptionIndex();
        if (regexIndex=="0")
            regexIndex=RecommendReg;

        string tlogolink = vdlList.LogoLink;
        string tsourcelink = vdlList.SourceLink;
        //���֮ǰ��δƥ�䵽Logo����ô���ʽ����ԭ�����ַ�����
        if (vdlList.LogoLink.Contains("fclive_tvicon.png"))
        {
            tlogolink="([^\"]*)";
            tsourcelink="(http|https)://\\S+\\.m3u8(\\?(.*?))?";
        }

        try
        {
            if (regexIndex.StartsWith("1")||regexIndex.StartsWith("2")||regexIndex.StartsWith("3")||regexIndex=="5")
            {
                return null;
            }
            else if (regexIndex=="4")
            {
                string reg = "(.*?),?((tvg-logo=\""+tlogolink+"\")(.*?)),("+vdlList.SourceName+")(,)?(\n)?((http|https)://\\S+(.*?)(?=\n))";
                return Regex.Match(AllVideoData, reg).Groups[0].Value;
            }

        }
        catch(Exception)
        {

        }

        return "";
    }

         public string GetOLDStr(string videodata, string name, string link)
        {
            string oldvalue;
            try
            {
                Match tVResult = Regex.Match(videodata, Regex.Escape(name)+@"(?s)([\s\S]*?)"+Regex.Escape(link));
                oldvalue = tVResult.Value;
                if (oldvalue=="")
                    return null;
                int tncount = oldvalue.Split(name).Length;
                int tlcount = oldvalue.Split(link).Length;

                if (tncount>2&&tlcount>2)
                {
                    return null;
                }
                if (tncount>2)
                {
                    string tvdata = oldvalue.Remove(oldvalue.IndexOf(name), name.Length);
                    return GetOLDStr(tvdata, name, link);
                }
                if (tlcount>2)
                {
                    string tvdata = oldvalue.Remove(oldvalue.LastIndexOf(link), link.Length);
                    return GetOLDStr(tvdata, name, link);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return oldvalue;
        }
     */

    /// <summary>
    /// ���M3U�б����¼�
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void VideosList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        VideoList videoList = e.Item as VideoList;
        LoadVideoDetail(videoList.SourceLink, videoList.RecommendReg);
    }
    /// <summary>
    /// �Ҳ�M3U8�б����¼�
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void VideoDetailList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (!VDLToogleBtn.IsToggled)
        {
            VideoDetailList detail = e.Item as VideoDetailList;

            string readresult = await new VideoManager().DownloadAndReadM3U8File(M3U8PlayList, new string[] { detail.SourceName, detail.SourceLink });
            if (readresult!="")
            {
                await DisplayAlert("��ʾ��Ϣ", readresult, "ȷ��");
                return;
            }


            M3U8PlayList.Insert(0, new string[] { "Ĭ��", detail.SourceLink });
            string[] MOptions = new string[M3U8PlayList.Count];
            MOptions[0]="Ĭ��\n";
            string WantPlayURL = detail.SourceLink;

            if (M3U8PlayList.Count > 2)
            {
                for (int i = 1; i<M3U8PlayList.Count; i++)
                {
                    MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
                }

                string MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
                if (MSelectResult == "ȡ��"||MSelectResult is null)
                {
                    return;
                }
                else if (!MSelectResult.Contains("Ĭ��"))
                {
                    int tmindex = Convert.ToInt32(MSelectResult.Remove(0, 1).Split("��")[0]);
                    WantPlayURL=M3U8PlayList[tmindex][1];
                }

            }


            new VideoManager().UpdatePrevPagePlaylist(M3U8PlayList);
            VideoPrevPage.videoPrevPage.VideoWindow.Source=WantPlayURL;
            VideoPrevPage.videoPrevPage.VideoWindow.Play();
            VideoPrevPage.videoPrevPage.NowPlayingTb.Text=detail.SourceName;


            var mainpage = ((Shell)App.Current.MainPage);
            mainpage.CurrentItem = mainpage.Items.FirstOrDefault();
            await mainpage.Navigation.PopToRootAsync();


            /*
             
            int permResult = await new APPPermissions().CheckAndReqPermissions();
            if (permResult!=0)
            {
                await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ�����ļ���", "ȷ��");
                return;
            }

            //���ݲ�ͬƽ̨ѡ��ͬ�Ļ��淽ʽ
            string cachePath;
#if WINDOWS
            cachePath = Path.Combine(FileSystem.AppDataDirectory+"/LiveStreamCache");
#elif ANDROID
            //var test= Directory.CreateDirectory(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DataDirectory.AbsolutePath)+"/LiveStreamCache");
            cachePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Android", "data", Android.App.Application.Context.PackageName+"/LiveStreamCache");
#else
            //��ʱ����ƻ���豸�Լ�����ƽ̨����ֱ��Դ����

            VideoPrevPage.videoPrevPage.VideoWindow.Source=detail.SourceLink;
            VideoPrevPage.videoPrevPage.VideoWindow.Play();
            VideoPrevPage.videoPrevPage.NowPlayingTb.Text=detail.SourceName;

            return;
#endif


            try
            {
                Directory.CreateDirectory(cachePath);
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�ޡ�", "ȷ��");
                return;
            }


            int statusCode;
            using (HttpClient httpClient = new HttpClient())
            {

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
                HttpResponseMessage response = null;

                try
                {
                    response = await httpClient.GetAsync(detail.SourceLink);

                    statusCode=(int)response.StatusCode;
                    if (!response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("��ʾ��Ϣ", "��ȡ�ļ�ʧ�ܣ����Ժ����ԣ�\n"+"HTTP������룺"+statusCode, "ȷ��");
                        return;
                    }
                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�޷����ӵ��Է�����������������������߸���һ��ֱ��Դ��", "ȷ��");
                    return;
                }


                detail.SourceName=detail.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");
                string FullM3U8Path = cachePath+"/"+detail.SourceName+".m3u8";
                try
                {
                    File.WriteAllText(FullM3U8Path, await response.Content.ReadAsStringAsync());

                    string[] result = File.ReadAllLines(FullM3U8Path);
                    for (int i = 0; i<result.Length; i++)
                    {
                        if (result[i].StartsWith("#")||String.IsNullOrEmpty(result[i])||String.IsNullOrWhiteSpace(result[i]))
                            continue;
                        else if (!result[i].Contains("://"))
                        {
                            result[i]=detail.SourceLink.Substring(0, detail.SourceLink.LastIndexOf("/")+1)+result[i];
                        }
                    }

                    File.WriteAllLines(FullM3U8Path, result);
                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�ޡ�", "ȷ��");
                    return;
                }


                VideoPrevPage.videoPrevPage.VideoWindow.Source=detail.SourceLink;
                VideoPrevPage.videoPrevPage.VideoWindow.Play();
                VideoPrevPage.videoPrevPage.NowPlayingTb.Text=detail.SourceName;

            }

             */


        }
    }
    /// <summary>
    /// ����M3U����
    /// </summary>
    public async void LoadVideos()
    {
        VideosListRing.IsRunning=true;
        //δ���سɹ����������ݣ��Կɲ���ԭ��������
        try
        {
            //��ʱ����API���ȡ��ҳ����
            string videodata = await new HttpClient().GetStringAsync("https://fclivetool.com/api/GetVList");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoList>));

            //��ʱ����С��15�����
            CurrentVideosList = (List<VideoList>)xmlSerializer.Deserialize(new StringReader(videodata));
            CurrentVideosList=CurrentVideosList.Where(p => p.RecommendReg!="-1").ToList();
            VideosList.ItemsSource= CurrentVideosList.Take(VL_COUNT_PER_PAGE);

            //����
            VideosList.SelectedItem=null;
            VideoDetailList.ItemsSource=null;
            CurrentVURL="";

            //��ʱ����ҳ��С��1�����
            VLMaxPageIndex= (int)Math.Ceiling(CurrentVideosList.Count/15.0);
            SetPage(1);
            VLBackBtn.IsEnabled=false;
            VLNextBtn.IsEnabled=true;

        }
        catch (Exception)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʧ�ܣ����Ժ����ԣ�", "ȷ��");
        }

        VideosListRing.IsRunning=false;
    }
    /// <summary>
    /// ����M3U8����
    /// </summary>
    public async void LoadVideoDetail(string url, string reg)
    {
        VideoDetailListRing.IsRunning = true;
        try
        {
            //����ֱ��Դ�����
            isFinishM3U8VCheck=true;
            if (M3U8ValidCheckCTS!=null)
            {
                M3U8ValidCheckCTS.Cancel();
            }

            AllVideoData = await new HttpClient().GetStringAsync("https://fclivetool.com/api/APPGetVD?url="+url);

            RecommendReg=reg;
            CurrentVURL =url;
            VDLIfmText.Text="";
            //���������ᴥ��SelectedIndexChanged�¼��������ڽ�ˢ���б��ʱ��ѡ��ִ���¼��ں�������
            IgnoreSelectionEvents=true;
            RegexSelectBox.SelectedIndex = 0;
            IgnoreSelectionEvents=false;

            CurrentVideosDetailList =new RegexManager().DoRegex(AllVideoData, RecommendReg);
            VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
            SetVDLPage(1);
            MakeVideosDataToPage(CurrentVideosDetailList, 0);
        }
        catch (Exception)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʧ�ܣ����Ժ����ԣ�", "ȷ��");

            /*
                        var link = videoList.SourceLink;
                        if (link.Contains("githubusercontent.com")||link.Contains("github.com"))
                        {
                            await DisplayAlert("��ʾ��Ϣ", "�����ڵĵ�������GitHub���ܲ�˳��", "ȷ��");
                        }
                        else
                        {
                            await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʧ�ܣ����Ժ����ԣ�", "ȷ��");
                        }
            
             */
        }

        VideoDetailListRing.IsRunning = false;
    }

    /// <summary>
    /// ����M3U�б��ҳ��
    /// </summary>
    /// <param name="index"></param>
    public void SetPage(int index)
    {
        VLCurrentPageIndex=index;
        VLCurrentPage.Text=index+"/"+VLMaxPageIndex;
    }
    /// <summary>
    /// ����M3U8�б��ҳ��
    /// </summary>
    /// <param name="index"></param>
    public void SetVDLPage(int index)
    {
        VDLCurrentPageIndex=index;
        VDLCurrentPage.Text=index+"/"+VDLMaxPageIndex;
    }
    private void VLBackBtn_Clicked(object sender, EventArgs e)
    {
        if (VLCurrentPageIndex<=1)
        {
            return;
        }

        SetPage(VLCurrentPageIndex-1);

        int skipcount = (VLCurrentPageIndex-1)*VL_COUNT_PER_PAGE;
        VideosList.ItemsSource= CurrentVideosList.Skip(skipcount).Take(VL_COUNT_PER_PAGE);

        if (VLCurrentPageIndex<=1)
        {
            VLBackBtn.IsEnabled = false;
            VLNextBtn.IsEnabled = true;
        }
        else
        {
            VLBackBtn.IsEnabled = true;
            VLNextBtn.IsEnabled = true;
        }
    }

    private async void VLJumpBtn_Clicked(object sender, EventArgs e)
    {
        int TargetPage = 1;
        if (!int.TryParse(VLPageTb.Text, out TargetPage))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }
        if (TargetPage<1||TargetPage>VLMaxPageIndex)
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }


        SetPage(TargetPage);

        int skipcount = (VLCurrentPageIndex-1)*VL_COUNT_PER_PAGE;
        if (VLCurrentPageIndex==VLMaxPageIndex)
        {
            VideosList.ItemsSource= CurrentVideosList.Skip(skipcount).Take(CurrentVideosList.Count-skipcount);
        }
        else
        {
            VideosList.ItemsSource= CurrentVideosList.Skip(skipcount).Take(VL_COUNT_PER_PAGE);
        }

        if (VLCurrentPageIndex>=VLMaxPageIndex)
        {
            VLBackBtn.IsEnabled = true;
            VLNextBtn.IsEnabled = false;
        }
        else if (VLCurrentPageIndex<=1)
        {
            VLBackBtn.IsEnabled = false;
            VLNextBtn.IsEnabled = true;
        }
        else
        {
            VLBackBtn.IsEnabled = true;
            VLNextBtn.IsEnabled = true;
        }
    }

    private void VLNextBtn_Clicked(object sender, EventArgs e)
    {
        if (VLCurrentPageIndex>=VLMaxPageIndex)
        {
            return;
        }

        SetPage(VLCurrentPageIndex+1);

        int skipcount = (VLCurrentPageIndex-1)*VL_COUNT_PER_PAGE;
        if (VLCurrentPageIndex==VLMaxPageIndex)
        {
            VideosList.ItemsSource= CurrentVideosList.Skip(skipcount).Take(CurrentVideosList.Count-skipcount);
        }
        else
        {
            VideosList.ItemsSource= CurrentVideosList.Skip(skipcount).Take(VL_COUNT_PER_PAGE);
        }

        if (VLCurrentPageIndex>=VLMaxPageIndex)
        {
            VLBackBtn.IsEnabled = true;
            VLNextBtn.IsEnabled = false;
        }
        else
        {
            VLBackBtn.IsEnabled = true;
            VLNextBtn.IsEnabled = true;
        }
    }

    private void VDLBackBtn_Clicked(object sender, EventArgs e)
    {
        if (VDLCurrentPageIndex<=1)
        {
            return;
        }

        SetVDLPage(VDLCurrentPageIndex-1);


        MakeVideosDataToPage(CurrentVideosDetailList, (VDLCurrentPageIndex - 1) * VDL_COUNT_PER_PAGE);

    }

    private async void VDLJumpBtn_Clicked(object sender, EventArgs e)
    {
        int TargetPage = 1;
        if (!int.TryParse(VDLPageTb.Text, out TargetPage))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }
        if (TargetPage<1||TargetPage>VDLMaxPageIndex)
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }


        SetVDLPage(TargetPage);

        MakeVideosDataToPage(CurrentVideosDetailList, (VDLCurrentPageIndex - 1) * VDL_COUNT_PER_PAGE);
    }

    private void VDLNextBtn_Clicked(object sender, EventArgs e)
    {
        if (VDLCurrentPageIndex>=VDLMaxPageIndex)
        {
            return;
        }

        SetVDLPage(VDLCurrentPageIndex+1);

        MakeVideosDataToPage(CurrentVideosDetailList, (VDLCurrentPageIndex-1)*VDL_COUNT_PER_PAGE);
    }

    public void MakeVideosDataToPage(List<VideoDetailList> list, int skipcount)
    {
        if (list.Count()<1)
        {
            VDLIfmText.Text="����տ���Ҳ�������һ������������~";

            VideoDetailList.ItemsSource=new List<VideoDetailList>() { };
            //�������ֱ�Ӹ�ֵnull������Ҫ�ڲ��ְ�ť����¼���ֱ�ʹ��CurrentVURL��ItemSource��Count���жϡ�
            //VideoDetailList.ItemsSource=null;

            return;
        }

        if (VDLCurrentPageIndex==VDLMaxPageIndex)
        {
            VideoDetailList.ItemsSource=list.Skip(skipcount).Take(list.Count-skipcount);
        }
        else
        {
            VideoDetailList.ItemsSource=list.Skip(skipcount).Take(VDL_COUNT_PER_PAGE);
        }

        if (VDLCurrentPageIndex>=VDLMaxPageIndex)
        {
            VDLBackBtn.IsEnabled = true;
            VDLNextBtn.IsEnabled = false;
        }
        else if (VDLCurrentPageIndex<=1)
        {
            VDLBackBtn.IsEnabled = false;
            VDLNextBtn.IsEnabled = true;
        }
        else
        {
            VDLBackBtn.IsEnabled = true;
            VDLNextBtn.IsEnabled = true;
        }
    }

    private void VLRefreshBtn_Clicked(object sender, EventArgs e)
    {
        LoadVideos();
    }

    private void VideosList_Refreshing(object sender, EventArgs e)
    {       
        //��ʹ��ListView�Լ��ļ���Ȧ
        VideosList.IsRefreshing=false;

        LoadVideos();
    }

    private void VLToolbarBtn_Clicked(object sender, EventArgs e)
    {
        //�ջ�
        if (VideosListPanel.TranslationY==0)
        {
            var animation = new Animation(v => VideosListPanel.TranslationY  = v, 0, -1000);
            animation.Commit(this, "VLPAnimation", 16, 500, Easing.CubicInOut);

            VideosList.IsEnabled = true;
        }
        //չ��
        else
        {
            var animation = new Animation(v => VideosListPanel.TranslationY  = v, -1000, 0);
            animation.Commit(this, "VLPAnimation", 16, 500, Easing.CubicOut);

            VideosList.IsEnabled = false;
        }

    }

    private void VDLRefreshBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            return;
        }

        LoadVideoDetail(CurrentVURL, RecommendReg);
    }

    private void VideoDetailList_Refreshing(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            return;
        }

        //��ʹ��ListView�Լ��ļ���Ȧ
        VideoDetailList.IsRefreshing=false;

        LoadVideoDetail(CurrentVURL, RecommendReg);
    }

    private void VDLToolbarBtn_Clicked(object sender, EventArgs e)
    {

        //�ջ�
        if (VideoDataListPanel.TranslationY==0)
        {
            var animation = new Animation(v => VideoDataListPanel.TranslationY  = v, 0, -1000);
            animation.Commit(this, "VLPAnimation", 16, 500, Easing.CubicInOut);

            VideoDetailList.IsEnabled=true;
        }
        //չ��
        else
        {
            var animation = new Animation(v => VideoDataListPanel.TranslationY  = v, -1000, 0);
            animation.Commit(this, "VLPAnimation", 16, 500, Easing.CubicOut);

            VideoDetailList.IsEnabled=false;
        }
    }
    private void RegexSelectBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ�", "ȷ��");
            return;
        }
        if (IgnoreSelectionEvents)
        {
            return;
        }

        VDLIfmText.Text="";
        string treg;

        string regexIndex = new RegexManager().GetRegexOptionIndex(RegexOptionCB.IsChecked, RegexSelectBox.SelectedIndex.ToString());
        if (regexIndex!="0")
        {
            treg=regexIndex;
        }
        else
        {
            treg=RecommendReg;
        }

        CurrentVideosDetailList = new RegexManager().DoRegex(AllVideoData, treg);
        VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
        SetVDLPage(1);

        MakeVideosDataToPage(CurrentVideosDetailList, 0);

    }

    private void RegexSelectTipBtn_Clicked(object sender, EventArgs e)
    {
        //���ļ���ȡ���ӵ��ı�
        /*
                 using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FCLiveToolApplication.Resources.Raw.regex_option_help"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        DisplayAlert("������Ϣ", reader.ReadToEnd(), "�ر�");
                    }
                }
         */
        ;
       
        DisplayAlert("������Ϣ", new MsgManager().GetRegexOptionTip(), "�ر�");

    }

    private async void SaveM3UFileBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ�����ļ���", "ȷ��");
            return;
        }


        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(AllVideoData)))
        {
            try
            {
                /*
                                 string sname;
                                if (VideosList.SelectedItem is null)
                                {
                                    sname="FileName";
                                }
                                else
                                {
                                    VideoList selectVL = VideosList.SelectedItem as VideoList;
                                    sname=selectVL.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");
                                }
                 */
                VideoList selectVL = VideosList.SelectedItem as VideoList;
                selectVL.SourceName=selectVL.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");
                var fileSaver = await FileSaver.SaveAsync(FileSystem.AppDataDirectory, selectVL.SourceName+".m3u", ms, CancellationToken.None);

                if (fileSaver.IsSuccessful)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n"+fileSaver.FilePath, "ȷ��");
                }
                else
                {
                    //��ʱ�ж�Ϊ�û���ѡ��Ŀ¼ʱ�����ȡ����ť
                    await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�ޡ�", "ȷ��");
            }
        }
    }

    private async void M3U8ValidBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

        var vdlcount = VideoDetailList.ItemsSource.Cast<VideoDetailList>().Count();
        if (vdlcount<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û��ֱ��Դ���볢�Ը���һ������������", "ȷ��");
            return;
        }
        if (!isFinishM3U8VCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ����ִ��ֱ��Դ��⣡", "ȷ��");
            return;
        }
        bool tSelect = await DisplayAlert("��ʾ��Ϣ", "���ν�Ҫ��� "+CurrentVideosDetailList.Count+" ��ֱ���źţ���ȷ��Ҫ��ʼ������\nȫ���������Ż��Զ����½����", "ȷ��", "ȡ��");
        if (!tSelect)
        {
            return;
        }

        M3U8ValidCheckCTS=new CancellationTokenSource();
        isFinishM3U8VCheck=false;
        M3U8ValidStopBtn.IsEnabled=true;
        M3U8VProgressText.Text="0 / "+CurrentVideosDetailList.Count;
        M3U8VCheckFinishCount = 0;
        RegexSelectBox.IsEnabled=false;
        ShowLoadOrRefreshDialog=false;


        M3U8ValidCheck(CurrentVideosDetailList);

        while (M3U8VCheckFinishCount<CurrentVideosDetailList.Count)
        {
            if (M3U8ValidCheckCTS.IsCancellationRequested)
            {
                break;
            }
            await Task.Delay(1000);
        }

        M3U8ValidStopBtn.IsEnabled=false;
        isFinishM3U8VCheck=true;
        RegexSelectBox.IsEnabled=true;
        //����Ҫ���÷�ҳ��Ϣ���˴�����Ҫָ��ҳ��
        VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
        SetVDLPage(1);

        if (!M3U8ValidCheckCTS.IsCancellationRequested)
        {
            MakeVideosDataToPage(CurrentVideosDetailList, 0);

            await DisplayAlert("��ʾ��Ϣ", "��ȫ�������ɣ�", "ȷ��");
        }
        else
        {
            if (ShowLoadOrRefreshDialog)
            {
                bool tresult = await DisplayAlert("��ʾ��Ϣ", "��Ҫ�鿴���ּ����Ľ������Ҫ���¼����б�", "�鿴���", "���¼���");
                if (tresult)
                {
                    MakeVideosDataToPage(CurrentVideosDetailList, 0);
                }
                else
                {
                    GetCurrentIndexAndLoadData(0);
                }
            }
            else
            {
                GetCurrentIndexAndLoadData(0);
                await DisplayAlert("��ʾ��Ϣ", "����ȡ����⣡", "ȷ��");
            }

            M3U8VProgressText.Text="";

        }


    }
    public async void M3U8ValidCheck(List<VideoDetailList> videodetaillist)
    {
        object obj = new object();
        for (int i = 0; i<videodetaillist.Count; i++)
        {
            var vd = videodetaillist[i];
            Thread thread = new Thread(async () =>
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout=TimeSpan.FromMinutes(2);

                    int statusCode;
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(DEFAULT_USER_AGENT);
                    HttpResponseMessage response = null;

                    try
                    {
                        //ȡ������
                        M3U8ValidCheckCTS.Token.ThrowIfCancellationRequested();

                        vd.HTTPStatusCode="Checking...";
                        vd.HTTPStatusTextBKG=Colors.Gray;

                        response = await httpClient.GetAsync(vd.SourceLink, M3U8ValidCheckCTS.Token);

                        statusCode=(int)response.StatusCode;
                        if (response.IsSuccessStatusCode)
                        {
                            vd.HTTPStatusCode="OK";
                            vd.HTTPStatusTextBKG=Colors.Green;
                        }
                        else
                        {
                            vd.HTTPStatusCode=statusCode.ToString();
                            vd.HTTPStatusTextBKG=Colors.Orange;
                        }

                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception)
                    {
                        vd.HTTPStatusCode="ERROR";
                        vd.HTTPStatusTextBKG=Colors.Red;
                    }

                    if (!M3U8ValidCheckCTS.IsCancellationRequested)
                    {
                        lock(obj)
                        {
                            M3U8VCheckFinishCount++;

                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                M3U8VProgressText.Text=M3U8VCheckFinishCount+" / "+videodetaillist.Count;
                            });
                        }


                    }

                }
            });
            thread.Start();

            await Task.Delay(20);
        }
    }
    private async void M3U8ValidRemoveBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

        var vdlcount = VideoDetailList.ItemsSource.Cast<VideoDetailList>().Count();
        if (vdlcount<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û��ֱ��Դ���볢�Ը���һ������������", "ȷ��");
            return;
        }
        if (!isFinishM3U8VCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ����ִ��ֱ��Դ��⣡", "ȷ��");
            return;
        }
        var notokcount = CurrentVideosDetailList.Where(p => (p.HTTPStatusCode!="OK")&&(p.HTTPStatusCode!=null)).Count();
        if (notokcount<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û����Ч��ֱ���źţ����������", "ȷ��");
            return;
        }
        bool tSelect = await DisplayAlert("��ʾ��Ϣ", "���ν�Ҫ�Ƴ� "+notokcount+" ����Ч��ֱ���źţ���ȷ���Ƴ���\n�Ƴ���ɵ��ҳ�����Ͻǵı��水ť��", "ȷ��", "ȡ��");
        if (!tSelect)
        {
            return;
        }

        //int tNOKRemoveCount = 0;
        //int tOKRemoveCount = 0;
        CurrentVideosDetailList.ForEach(p =>
        {
            if (p.HTTPStatusCode!="OK"&&p.HTTPStatusCode!=null)
            {
                AllVideoData=AllVideoData.Replace(p.FullM3U8Str, "");

                /*
                                 string vname = p.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");

                string m3u8Str = GetFullM3U8Str(p);
                if (m3u8Str is "")
                {
                    //�����Ȼ�����������������ַ������ǾͲ������滻
                    tNOKRemoveCount++;
                }
                else if (m3u8Str is null)
                {
                    AllVideoData=AllVideoData.Replace(p.FullM3U8Str, "");
                    tOKRemoveCount++;
                }
                else
                {
                    AllVideoData=AllVideoData.Replace(m3u8Str, "");
                    tOKRemoveCount++;
                }
                 */
            }
        });
        CurrentVideosDetailList.RemoveAll(p => p.HTTPStatusCode!="OK"&&p.HTTPStatusCode!=null);

        VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
        SetVDLPage(1);
        MakeVideosDataToPage(CurrentVideosDetailList, 0);

        //await DisplayAlert("��ʾ��Ϣ", "�ѳɹ����б����Ƴ�������Ч��ֱ���źţ�\n�Ѵ�M3U�ļ��������Ƴ���Ч��ֱ���ź�"+tOKRemoveCount+"����δ�ɹ��Ƴ�"+tNOKRemoveCount+"����", "ȷ��");
        await DisplayAlert("��ʾ��Ϣ", "�ѳɹ����б����Ƴ�������Ч��ֱ���źţ�", "ȷ��");

    }

    private async void M3U8ValidStopBtn_Clicked(object sender, EventArgs e)
    {
        if (M3U8ValidCheckCTS!=null)
        {
            bool CancelCheck = await DisplayAlert("��ʾ��Ϣ", "��Ҫֹͣ�����ֹͣ����ʱ��֧�ָֻ����ȡ�", "ȷ��", "ȡ��");
            if (CancelCheck)
            {
                M3U8ValidCheckCTS.Cancel();
                //������ע�͵�
                M3U8ValidStopBtn.IsEnabled=false;

                ShowLoadOrRefreshDialog = true;
            }

        }
    }
    public void GetCurrentIndexAndLoadData(int skipcount)
    {
        string regexIndex = new RegexManager().GetRegexOptionIndex(RegexOptionCB.IsChecked, RegexSelectBox.SelectedIndex.ToString());
        if (regexIndex!="0")
        {
            MakeVideosDataToPage(new RegexManager().DoRegex(AllVideoData, regexIndex), skipcount);
        }
        else
        {
            MakeVideosDataToPage(new RegexManager().DoRegex(AllVideoData, RecommendReg), skipcount);
        }
    }

    private void VideoDetailList_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            VideoDetailList.SelectedItem=null;

            if (VideoDetailList.ItemsSource is not null&&VideoDetailList.ItemsSource.Cast<VideoDetailList>().Count()>0)
            {
                VDLPagePanel.IsVisible=true;
            }
            else
            {
                VDLPagePanel.IsVisible=false;
            }
        }

    }

    private async void VDLSearchBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ������������������ݣ�", "ȷ��");
            return;
        }
        if (!isFinishM3U8VCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ����ִ��ֱ��Դ��⣬����ֹͣ�����������", "ȷ��");
            return;
        }

        string searchText = VDLSearchTb.Text;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            await DisplayAlert("��ʾ��Ϣ", "�����������Ч��", "ȷ��");
            return;
        }
        /*
                 if (searchText.Length < 3)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�������ݳ��Ȳ���С��3��", "ȷ��");
                    return;
                }
         */

        VDLIfmText.Text="";
        string treg;
        string regexIndex = new RegexManager().GetRegexOptionIndex(RegexOptionCB.IsChecked, RegexSelectBox.SelectedIndex.ToString());
        if (regexIndex!="0")
        {
            treg=regexIndex;
        }
        else
        {
            treg=RecommendReg;
        }

        List<VideoDetailList> tlist = new RegexManager().DoRegex(AllVideoData, treg);
        if (tlist.Count<1)
        {
            tlist = new RegexManager().DoRegex(AllVideoData, RecommendReg);
            DisplayAlert("��ʾ��Ϣ", "��ǰ��������δ�ܽ�����ֱ��Դ���Ѹ�Ϊ�Ƽ��ķ���ȥ������ִ��������", "ȷ��");
        }
        //��ʱ����д��������
        CurrentVideosDetailList= tlist.Where(p => p.SourceName.Contains(searchText)).ToList();

        VDLMaxPageIndex= (int)Math.Ceiling(CurrentVideosDetailList.Count/100.0);
        SetVDLPage(1);

        MakeVideosDataToPage(CurrentVideosDetailList, 0);

    }

    private async void JumpEditM3UBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentVURL=="")
        {
            await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }


        List<VideoEditList> videoEditLists = await new VideoManager().ReadM3UString(AllVideoData);

        var mainpage = ((Shell)App.Current.MainPage);
        mainpage.CurrentItem = mainpage.Items.FirstOrDefault(p => p.Title=="ֱ��Դ�༭");
        await mainpage.Navigation.PopToRootAsync();

        VideoEditPage.videoEditPage.VideoEditList.ItemsSource=videoEditLists;
    }
}