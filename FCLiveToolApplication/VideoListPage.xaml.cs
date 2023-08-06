using Microsoft.Maui.Platform;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Microsoft.Maui.Controls.Button.ButtonContentLayout;
using static Microsoft.Maui.Controls.Button;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls.PlatformConfiguration;
using CommunityToolkit.Maui.Storage;
using System.Text;
using Encoding = System.Text.Encoding;
using System.Net;
using System;
using CommunityToolkit.Maui.Views;

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
    public List<string[]> M3U8PlayList = new List<string[]>();
    public bool isFinishMValidCheck = true;

    CancellationTokenSource M3U8ValidCheckCTS;

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

        var tresult = GetOLDStr(AllVideoData, vname, selectVDL.SourceLink.Replace("\r", "").Replace("\n", "").Replace("\r\n", ""));
        if (tresult is null||tresult.Contains("#EXTINF"))
        {
            await DisplayAlert("��ʾ��Ϣ", "�޷����µ�ǰֱ��Դ����ΪM3U�ļ��ڰ�������͵�ǰֱ��Դ��ͬ������+URL��", "ȷ��");
            return;
        }

        AllVideoData=AllVideoData.Replace(tresult, tresult.Replace(vname, newvalue));
        await DisplayAlert("��ʾ��Ϣ", "���³ɹ���", "ȷ��");

        VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
        //MAUI��ListView�Ŀӣ����¸�ֵ����Դ��SelectedItem��Ȼ�����Զ����
        VideoDetailList.SelectedItem=null;

    }
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
            var tresult = GetOLDStr(AllVideoData, vname, selectVDL.SourceLink.Replace("\r", "").Replace("\n", "").Replace("\r\n", ""));
            if (tresult is null||tresult.Contains("#EXTINF"))
            {
                await DisplayAlert("��ʾ��Ϣ", "�޷��Ƴ���ǰֱ��Դ����ΪM3U�ļ��ڰ�������͵�ǰֱ��Դ��ͬ������+URL��", "ȷ��");
                return;
            }

            AllVideoData=AllVideoData.Replace(tresult, "");
            await DisplayAlert("��ʾ��Ϣ", "�Ƴ��ɹ���", "ȷ��");

            VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
            //MAUI��ListView�Ŀӣ����¸�ֵ����Դ��SelectedItem��Ȼ�����Զ����
            VideoDetailList.SelectedItem=null;
        }

    }

    public string GetOLDStr(string videodata, string name, string link)
    {
        string oldvalue;
        try
        {
            Match tVResult = Regex.Match(videodata, Regex.Escape(name)+@"(?s)([\s\S]*?)"+Regex.Escape(link));
            oldvalue = tVResult.Value;
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
            VideosList.ItemsSource= CurrentVideosList.Take(15);
            //��������Դ�����ֶ����ѡ�е�����Ϣ
            VideosList.SelectedItem=null;

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
            isFinishMValidCheck=true;

            AllVideoData = await new HttpClient().GetStringAsync("https://fclivetool.com/api/APPGetVD?url="+url);

            RecommendReg=reg;
            CurrentVURL =url;

            //���������ᴥ��SelectedIndexChanged�¼��������ڽ�ˢ���б��ʱ��ѡ��ִ���¼��ں�������
            IgnoreSelectionEvents=true;
            RegexSelectBox.SelectedIndex = 0;
            IgnoreSelectionEvents=false;

            VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
            //��������Դ�����ֶ����ѡ�е�����Ϣ
            VideoDetailList.SelectedItem=null;
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

        VideoDetailListRing.IsRunning = false;
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
        for (int i = 0; i<match.Count; i++)
        {
            VideoDetailList videoDetail = new VideoDetailList()
            {
                //ID��̨�̨꣬����ֱ��Դ��ַ
                Id=i,
                LogoLink=match[i].Groups[UseGroup[0]].Value=="" ? "fclive_tvicon.png" : match[i].Groups[UseGroup[0]].Value,
                SourceName=match[i].Groups[UseGroup[1]].Value,
                SourceLink=match[i].Groups[UseGroup[2]].Value,
                isHTTPS=match[i].Groups[UseGroup[2]].Value.ToLower().StartsWith("https://") ? true : false,
                FileName=Regex.Match(match[i].Groups[UseGroup[2]].Value, @"\/([^\/]+\.m3u8)").Groups[1].Value
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

    private void VideoDetailList_Refreshing(object sender, EventArgs e)
    {
        LoadVideoDetail(CurrentVURL, RecommendReg);

        //��ʹ��ListView�Լ��ļ���Ȧ
        VideoDetailList.IsRefreshing=false;
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

    private async void SaveM3UFileBtn_Clicked(object sender, EventArgs e)
    {
        /*
                 if (CurrentVURL=="")
                {
                    await DisplayAlert("��ʾ��Ϣ", "���������M3U�б���ѡ��һ��ֱ��Դ��", "ȷ��");
                    return;
                }
         */
        //���ˢ��������б����ݲ�֧�ֱ���M3U�ļ�
        if (VideosList.SelectedItem is null)
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
        if (!isFinishMValidCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰֱ��Դδ��ɼ�⣡", "ȷ��");
            return;
        }
        bool tSelect= await DisplayAlert("��ʾ��Ϣ", "���ν�Ҫ���"+vdlcount+"��ֱ���źţ���ȷ��Ҫ��ʼ������\nȫ���������Ż��Զ����½����", "ȷ��","ȡ��");
        if (!tSelect)
        {
            return;
        }

        M3U8ValidCheckCTS=new CancellationTokenSource();
        isFinishMValidCheck=false;
        M3U8ValidStopBtn.IsEnabled=true;

        int finishcheck = 0;
        VideoDetailList.ItemsSource.Cast<VideoDetailList>().ToList().ForEach(async p =>
        {
            Thread thread = new Thread(async ()=>
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        int statusCode;
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(@"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
                        HttpResponseMessage response = null;
                        //ȡ������
                        M3U8ValidCheckCTS.Token.ThrowIfCancellationRequested();

                        try
                        {
                            p.HTTPStatusCode="Checking...";
                            p.HTTPStatusTextBKG=Colors.Gray;

                            response = await httpClient.GetAsync(p.SourceLink,M3U8ValidCheckCTS.Token);

                            statusCode=(int)response.StatusCode;
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    p.HTTPStatusCode="OK";
                                    p.HTTPStatusTextBKG=Colors.Green;
                                }
                                else
                                {
                                    p.HTTPStatusCode=statusCode.ToString();
                                    p.HTTPStatusTextBKG=Colors.Orange;
                                }
                            });

                        }
                        catch (OperationCanceledException ex)
                        {

                        }
                        catch (Exception)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                p.HTTPStatusCode="ERROR";
                                p.HTTPStatusTextBKG=Colors.Red;
                            });
                        }

                        if(!M3U8ValidCheckCTS.IsCancellationRequested)
                        {
                            finishcheck++;
                        }

                    }
                });
            thread.Start();

            await Task.Delay(20);
        });
 
        while(finishcheck<vdlcount)
        {
            if (M3U8ValidCheckCTS.IsCancellationRequested)
            {
                break;
            }
            await Task.Delay(1000);
        }

        M3U8ValidStopBtn.IsEnabled=false;
        isFinishMValidCheck=true;

        if (!M3U8ValidCheckCTS.IsCancellationRequested)
        {
            var tlist = VideoDetailList.ItemsSource;
            VideoDetailList.ItemsSource=null;
            VideoDetailList.ItemsSource=tlist;

            await DisplayAlert("��ʾ��Ϣ", "��ȫ�������ɣ�", "ȷ��");
        }
        else
        {
            string regexIndex = GetRegexOptionIndex();
            if (regexIndex!="0")
            {
                VideoDetailList.ItemsSource= DoRegex(AllVideoData, regexIndex);
            }
            else
            {
                VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
            }
            await DisplayAlert("��ʾ��Ϣ", "����ȡ����⣡", "ȷ��");
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
        if (!isFinishMValidCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰֱ��Դδ��ɼ�⣡", "ȷ��");
            return;
        }
        var notokcount = VideoDetailList.ItemsSource.Cast<VideoDetailList>().Where(p=>(p.HTTPStatusCode!="OK")&&(p.HTTPStatusCode!=null)).Count();
        if (notokcount<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û����Ч��ֱ���źţ����������", "ȷ��");
            return;
        }
        bool tSelect = await DisplayAlert("��ʾ��Ϣ", "���ν�Ҫ�Ƴ�"+notokcount+"����Ч��ֱ���źţ���ȷ���Ƴ���\n�Ƴ���ɵ��ҳ�����Ͻǵı��水ť��", "ȷ��", "ȡ��");
        if (!tSelect)
        {
            return;
        }


        VideoDetailList.ItemsSource.Cast<VideoDetailList>().ToList().ForEach(p =>
        {
            if(p.HTTPStatusCode!="OK"&&p.HTTPStatusCode!=null)
            {
                string vname = p.SourceName.Replace("\r", "").Replace("\n", "").Replace("\r\n", "");

                var tresult = GetOLDStr(AllVideoData, vname, p.SourceLink.Replace("\r", "").Replace("\n", "").Replace("\r\n", ""));
                if (tresult != null&&!tresult.Contains("#EXTINF"))
                {
                    AllVideoData=AllVideoData.Replace(tresult, "");
                }

            }
        });

        string regexIndex = GetRegexOptionIndex();
        if (regexIndex!="0")
        {
            VideoDetailList.ItemsSource= DoRegex(AllVideoData, regexIndex);
        }
        else
        {
            VideoDetailList.ItemsSource= DoRegex(AllVideoData, RecommendReg);
        }
        await DisplayAlert("��ʾ��Ϣ", "�ѳɹ��Ƴ���Ч��ֱ���źţ�", "ȷ��");

    }

    private async void M3U8ValidStopBtn_Clicked(object sender, EventArgs e)
    {
        if(M3U8ValidCheckCTS!=null)
        {
            bool CancelCheck =await DisplayAlert("��ʾ��Ϣ", "��Ҫֹͣ�����ֹͣ����ʱ��֧�ָֻ����ȡ�", "ȷ��", "ȡ��");
            if (CancelCheck)
            {
                M3U8ValidCheckCTS.Cancel();
                //������ע�͵�
                M3U8ValidStopBtn.IsEnabled=false;
            }

        }
    }
}