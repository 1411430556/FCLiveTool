using Microsoft.Maui.Platform;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Microsoft.Maui.Controls.Button.ButtonContentLayout;
using static Microsoft.Maui.Controls.Button;
using static System.Net.WebRequestMethods;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using CommunityToolkit.Maui.Storage;
using System.Text;
using Encoding = System.Text.Encoding;
using System.Net;


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
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        LoadVideos();
        InitRegexList();
        DeviceDisplay.MainDisplayInfoChanged+=DeviceDisplay_MainDisplayInfoChanged;
    }
    private void DeviceDisplay_MainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
    {
#if ANDROID

        if (DeviceDisplay.Current.MainDisplayInfo.Orientation==DisplayOrientation.Landscape)
        {

        }
        else
        {

        }
#endif
    }
    /// <summary>
    /// ��ʼ�������б�
    /// </summary>
    public void InitRegexList()
    {
        RegexSelectBox.BindingContext = this;
        RegexOption = new List<string>() { "�Զ�ƥ��", "����1", "����2", "����3", "����4", "����5" };
        RegexSelectBox.ItemsSource = RegexOption;

        //RegexSelectBox.SelectedIndex = 0;
    }

    public int VLCurrentPageIndex = 1;
    public int VLMaxPageIndex;
    public const int VL_COUNT_PER_PAGE = 15;
    public string AllVideoData;
    public int[] UseGroup;
    public List<VideoList> CurrentVideosList;
    public string CurrentVURL = "";
    public string RecommendReg = "0";
    public List<string> RegexOption;
    public bool IgnoreSelectionEvents = false;

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

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
            /*
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, selectVDL.SourceLink);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
           */

            HttpResponseMessage response=null;

            try
            {
                response = await httpClient.GetAsync(selectVDL.SourceLink);

                statusCode=(int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ����Ժ����ԣ�", "ȷ��");
                    return;
                }
            }
            catch(Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "���ӶԷ�������ʧ�ܣ��������磡", "ȷ��");
                return;
            }

            //var httpstream = await httpClient.GetStreamAsync(selectVDL.SourceLink)
            using (var httpstream = await response.Content.ReadAsStreamAsync())
            {
                var fileSaver = await FileSaver.SaveAsync(FileSystem.AppDataDirectory, selectVDL.SourceName+".m3u8", httpstream, CancellationToken.None);

                if (fileSaver.IsSuccessful)
                {
                    await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n"+fileSaver.FilePath, "ȷ��");
                }
                else
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
    private async void EditBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoDetailList.SelectedItem==null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

        //string appDataDirectory = FileSystem.AppDataDirectory;
    }
    private async void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoDetailList.SelectedItem==null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�������б���ѡ��һ��ֱ��Դ��", "ȷ��");
            return;
        }

    }

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
    private void VideoDetailList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (!VDLToogleBtn.IsToggled)
        {
            VideoDetailList detail = e.Item as VideoDetailList;

            VideoPrevPage.videoPrevPage.VideoWindow.Source=detail.SourceLink;
            VideoPrevPage.videoPrevPage.VideoWindow.Play();
            VideoPrevPage.videoPrevPage.NowPlayingTb.Text=detail.SourceName;
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
            VideosList.ItemsSource= CurrentVideosList.Take(15);

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
        VideoDataListRing.IsRunning = true;
        try
        {
            AllVideoData = await new HttpClient().GetStringAsync("https://fclivetool.com/api/APPGetVD?url="+url);

            RecommendReg=reg;
            CurrentVURL =url;

            //���������ᴥ��SelectedIndexChanged�¼��������ڽ�ˢ���б��ʱ��ѡ��ִ���¼��ں�������
            IgnoreSelectionEvents=true;
            RegexSelectBox.SelectedIndex = 0;
            IgnoreSelectionEvents=false;

            VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);

            VDLIfmText.Text="";
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

        VideoDataListRing.IsRunning = false;
    }
    /// <summary>
    /// ��ȡ��ǰѡ��Ĺ�������
    /// </summary>
    /// <returns></returns>
    public string GetRegexOptionIndex()
    {
        bool isOnlyM3U8 = RegexOptionCB.IsChecked;
        switch (RegexSelectBox.SelectedIndex.ToString())
        {
            case "1":
                if (!isOnlyM3U8)
                {
                    return "1.2";
                }
                return "1";
            case "2":
                if (!isOnlyM3U8)
                {
                    return "2.2";
                }
                return "2";
            case "3":
                if (!isOnlyM3U8)
                {
                    return "3.2";
                }
                return "3";
            case "4":
                return "4";
            case "5":
                return "5";
            default:
                return "0";
        }

    }
    /// <summary>
    /// ʹ��������ʽ����ֱ��Դ���ݣ�ֱ��Դ�����ǰ������ɸ�M3U8ֱ��Դ���ַ���
    /// </summary>
    /// <param name="videodata">ֱ��Դ����</param>
    /// <param name="recreg">������ʽ</param>
    /// <returns>������ʽƥ������б�</returns>
    public List<VideoDetailList> DoRegex(string videodata, string recreg)
    {
        MatchCollection match = Regex.Matches(videodata, UseRegex(recreg));

        List<VideoDetailList> result = new List<VideoDetailList>();
        for (int i = 0; i<match.Count(); i++)
        {
            VideoDetailList videoDetail = new VideoDetailList()
            {
                //ID��̨�̨꣬����ֱ��Դ��ַ
                Id=i,
                LogoLink=match[i].Groups[UseGroup[0]].Value=="" ? "fclive_tvicon.png" : match[i].Groups[UseGroup[0]].Value,
                SourceName=match[i].Groups[UseGroup[1]].Value,
                SourceLink=match[i].Groups[UseGroup[2]].Value
            };
            //videoDetail.LogoLink=videoDetail.LogoLink=="" ? "fclive_tvicon.png" : videoDetail.LogoLink;

            result.Add(videoDetail);
        }

        return result;
    }
    /// <summary>
    /// �����ṩ����������ȡ��Ӧ��������ʽ
    /// </summary>
    /// <param name="index">����</param>
    /// <returns>������Ӧ��������ʽ</returns>
    public string UseRegex(string index)
    {
        switch (index)
        {
            case "1":
                UseGroup =new int[] { 1, 2, 3 };
                return @"(?:.*?tvg-logo=""([^""]*)"")?(?:.*?tvg-name=""([^""]*)"")?.*\r?\n?((http|https)://\S+\.m3u8(\?(.*?))?(?=\n|,))";
            //1.2Ϊ1�Ĳ�����M3U8��׺�İ汾
            case "1.2":
                UseGroup =new int[] { 1, 2, 3 };
                return @"(?:.*?tvg-logo=""([^""]*)"")?(?:.*?tvg-name=""([^""]*)"")?.*\r?\n?((http|https)://\S+(.*?)(?=\n|,))";
            //ֻ��2��������ƥ��̨����ƥ��̨��
            case "2":
                UseGroup =new int[] { 2, 1, 3 };
                return @"(?:.*?tvg-name=""([^""]*)"")(?:.*?tvg-logo=""([^""]*)"")?.*\r?\n?((http|https)://\S+\.m3u8(\?(.*?))?(?=\n|,))";
            //2.2Ϊ2�Ĳ�����M3U8��׺�İ汾
            case "2.2":
                UseGroup =new int[] { 2, 1, 3 };
                return @"(?:.*?tvg-name=""([^""]*)"")(?:.*?tvg-logo=""([^""]*)"")?.*\r?\n?((http|https)://\S+(.*?)(?=\n|,))";
            case "3":
                UseGroup =new int[] { 3, 5, 8 };
                return @"((tvg-logo=""([^""]*)"")(.*?))?,(.+?)(,)?(\n)?(?=((http|https)://\S+\.m3u8(\?(.*?))?(?=\n|,)))";
            //3.2Ϊ3�Ĳ�����M3U8��׺�İ汾
            case "3.2":
                UseGroup =new int[] { 3, 5, 8 };
                return @"((tvg-logo=""([^""]*)"")(.*?))?,(.+?)(,)?(\n)?(?=((http|https)://\S+(.*?)(?=\n|,)))";
            case "4":
                UseGroup =new int[] { 3, 5, 8 };
                return @",?((tvg-logo=""([^""]*)"")(.*?)),(.+?)(,)?(\n)?(?=((http|https)://\S+(.*?)(?=\n|,)))";
            case "5":
                UseGroup =new int[] { 2, 5, 7 };
                return @"(((http|https)://\S+)(,))?(.*?)(,)((http|https)://\S+(?=\n|,|\s{1}))";
            default:
                return "";
        }

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

    private void VLRefreshBtn_Clicked(object sender, EventArgs e)
    {
        LoadVideos();
    }

    private void VideosList_Refreshing(object sender, EventArgs e)
    {
        LoadVideos();

        //��ʹ��ListView�Լ��ļ���Ȧ
        VideosList.IsRefreshing=false;
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
        if(IgnoreSelectionEvents)
        {
            return;
        }

        VDLIfmText.Text="";
        string regexIndex = GetRegexOptionIndex();
        if (regexIndex!="0")
        {
            VideoDetailList.ItemsSource= DoRegex(AllVideoData, regexIndex);
        }
        else
        {
            VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
        }


        if (VideoDetailList.ItemsSource.Cast<VideoDetailList>().Count()<1)
        {
            VDLIfmText.Text="����տ���Ҳ�������һ������������~";
        }


    }

    private void RegexSelectIfmBtn_Clicked(object sender, EventArgs e)
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
        string showmsg = "���ڲ�ͬ��M3U�ļ�������ݸ�ʽ���в�ͬ������һ���ļ����ж��ֲ�ͬ�ĸ�ʽ����˱������ṩ�˶��ֽ����������������ܵ���������M3U�ļ�������ݡ�"+"\n"
        +"������������ֱ��Դû�����ƣ��޷������Լ�URL�д��������������Գ��Ը�������������"+"\n\n\n"
        +"���������и�ѡ��Ľ��ͣ�"+"\n\n"
        +"����ƥ��M3U8�ļ���������ѡ���ʾ����ƥ��ֱ��ԴURL���ļ���Ϊ��M3U8����׺���ļ��������ļ��򲻻�ȡ��"+"\n\n\n"
        +"���������й���Ľ��ͣ�"+"\n\n"
        +"����1"+"\n"
        +"ƥ�䣺̨��(tvg-logo)��̨��(tvg-name)��URL"+"\n\n"
        +"tvg-logo=\"̨��\"(��ѡ)"+"\n"
        +"tvg-name=\"̨��\"(��ѡ��������Ϊ��)"+"\n"
        +"\\r��\\n(��ѡ)"+"\n"
        +"http://��https://(+�����ַ�).m3u8?������=ֵ&������=ֵ...(ȫ����ѡ)"+"\n\n"
        +"****************************************"+"\n\n"
        +"����2"+"\n"
        +"���һ���෴��ƥ�䣺̨��(tvg-name)��̨��(tvg-logo)��URL"+"\n\n"
        +"tvg-name=\"̨��\""+"\n"
        +"tvg-logo=\"̨��\"(��ѡ)"+"\n"
        +"\\r��\\n(��ѡ)"+"\n"
        +"http://��https://(+�����ַ�).m3u8?������=ֵ&������=ֵ...(ȫ����ѡ)"+"\n\n"
        +"****************************************"+"\n\n"
        +"����3"+"\n"
        +"ƥ�䣺̨��(tvg-logo)��̨��(������֮���ı�)��URL"+"\n\n"
        +"tvg-logo=\"̨��\"(��ѡ)"+"\n"
        +","+"\n"
        +"̨��"+"\n"
        +",��\\n(��ѡ)"+"\n"
        +"http://��https://(+�����ַ�).m3u8?������=ֵ&������=ֵ...(ȫ����ѡ)"+"\n\n"
        +"****************************************"+"\n\n"
        +"����4"+"\n"
        +"�͵�������ͬ����������#EXTINF�ַ���̨���ַ�֮���ж������"+"\n\n"
        +","+"\n"
        +"tvg-logo=\"̨��\""+"\n"
        +","+"\n"
        +"̨��"+"\n"
        +",��\\n(��ѡ)"+"\n"
        +"http://��https://(+�����ַ�������.m3u8��׺)?������=ֵ&������=ֵ...(ȫ����ѡ)"+"\n\n"
        +"****************************************"+"\n\n"
        +"����5"+"\n"
        +"�򵥴ֱ����޸��Ӹ�ʽ��ƥ�䣺̨�̨꣬����URL"+"\n\n"
        +"̨��(��ѡ)"+"\n"
        +","+"\n"
        +"̨��"+"\n"
        +","+"\n"
        +"http://��https://(+�����ַ�������.m3u8��׺)?������=ֵ&������=ֵ...(ȫ����ѡ)";

        DisplayAlert("������Ϣ", showmsg, "�ر�");

    }
}