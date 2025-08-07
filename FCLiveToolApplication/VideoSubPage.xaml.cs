using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using FCLiveToolApplication.Popup;
using System.Xml.Serialization;

namespace FCLiveToolApplication;

public partial class VideoSubPage : ContentPage
{
    public VideoSubPage()
    {
        InitializeComponent();
    }

    public static VideoSubPage videoSubPage;
    public List<VideoDetailList> CurrentVideoSubDetailList = new List<VideoDetailList>();
    public string CurrentVSLName;
    public string AllVideoData;
    public int VSDLCurrentPageIndex = 1;
    public int VSDLMaxPageIndex;
    public const int VSDL_COUNT_PER_PAGE = 100;
    public const double D_VSDL_COUNT_PER_PAGE = 100.0;
    public int RegexSelectIndex = 2;
    public string RecommendRegex = "3";
    public bool RegexOption1 = false;
    public List<string[]> M3U8PlayList = new List<string[]>();
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (videoSubPage != null)
        {
            return;
        }

        videoSubPage=this;

        new Thread(async()=>
        {
            ReadLocalSubList();
        }).Start();

    }
    public async void ReadLocalSubList()
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ����Ͷ�ȡ�ļ����������Ȩ�������漰�ļ���д�Ĳ������޷�����ʹ�ã�", "ȷ��");
                return;
            });
        }

        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var localStr = File.ReadAllText(dataPath+"\\VideoSubList.log");
                    if (!string.IsNullOrWhiteSpace(localStr))
                    {
                        var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(localStr));

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            VideoSubList.ItemsSource = tlist;
                        });
                    }
                }
            }
            catch (Exception)
            {
                await MainThread.InvokeOnMainThreadAsync(async() =>
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ȡ��������ʱ����", "ȷ��");
                });

            }

        }

    }
    private void VideoSubListAddItemBtn_Clicked(object sender, EventArgs e)
    {
        VideoSubListPopup videoSubListPopup = new VideoSubListPopup(0);
        //videoSubListPopup.MainGrid.HeightRequest = Window.Height/1.5;
        videoSubListPopup.MainGrid.WidthRequest =Window.Width/1.5;
        this.ShowPopup(videoSubListPopup);
    }

    private void VSLEditBtn_Clicked(object sender, EventArgs e)
    {
        Button button=sender as Button;

        VideoSubListPopup videoSubListPopup = new VideoSubListPopup(1,button.CommandParameter.ToString());
        //videoSubListPopup.MainGrid.HeightRequest = Window.Height/1.5;
        videoSubListPopup.MainGrid.WidthRequest =Window.Width/1.5;
        this.ShowPopup(videoSubListPopup);
    }

    private async void VSLRemoveBtn_Clicked(object sender, EventArgs e)
    {
        Button button = sender as Button;
        string itemSubName = (button.BindingContext as VideoSubList).SubName;

        if (string.IsNullOrWhiteSpace(itemSubName))
        {
            await DisplayAlert("��ʾ��Ϣ", "�������������ԣ�", "ȷ��");
            return;
        }
        if (!await DisplayAlert("��ʾ��Ϣ","��Ҫɾ������ "+itemSubName+" ��", "ȷ��", "ȡ��"))
        {
            return;
        }


        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                    var items = tlist.FirstOrDefault(p => p.SubName==itemSubName);
                    if (items != null)
                    {
                        tlist.Remove(items);
                        RefreshVSL(tlist);

                        using (StringWriter sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, tlist);
                            File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                        }

                        await DisplayAlert("��ʾ��Ϣ", "ɾ�����ĳɹ���", "ȷ��");

                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�", "ȷ��");
                    }
                }
                else
                {
                    await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "��������ʱ������ˢ�����ԣ�", "ȷ��");
            }

        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
        }

    }

    private async void VSLEnabledUpdateToogle_Toggled(object sender, ToggledEventArgs e)
    {
        Switch button = sender as Switch;
        if (button.BindingContext is null)
        {
            return;
        }

        string itemSubName = (button.BindingContext as VideoSubList).SubName;
        if (string.IsNullOrWhiteSpace(itemSubName))
        {
            await DisplayAlert("��ʾ��Ϣ", "�������������ԣ�", "ȷ��");
            return;
        }


        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                    var items = tlist.FirstOrDefault(p => p.SubName==itemSubName);
                    if (items != null)
                    {
                        items.IsEnabledUpdate=button.IsToggled;
                        //����Ҫˢ�£�Ҳ�����˷�������
                        //RefreshVSL(tlist);

                        using (StringWriter sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, tlist);
                            File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                        }

                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�", "ȷ��");
                    }
                }
                else
                {
                    await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "��������ʱ������ˢ�����ԣ�", "ȷ��");
            }

        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
        }

    }

    public async void PopShowMsg(string msg)
    {
        await DisplayAlert("��ʾ��Ϣ", msg, "ȷ��");
    }
    public async Task<bool> PopShowMsgAndReturn(string msg)
    {
        return await DisplayAlert("��ʾ��Ϣ", msg, "ȷ��", "ȡ��");
    }
    public void RefreshVSL(List<VideoSubList> tlist)
    {
        VideoSubList.ItemsSource = tlist.Take(tlist.Count);
    }
    private async void VSLUpdateBtn_Clicked(object sender, EventArgs e)
    {
        Button button = sender as Button;
        string itemSubName = (button.BindingContext as VideoSubList).SubName;
        if (string.IsNullOrWhiteSpace(itemSubName))
        {
            await DisplayAlert("��ʾ��Ϣ", "�������������ԣ�", "ȷ��");
            return;
        }

        UpdateSubFunc(itemSubName);
    }
    public async void UpdateSubFunc(string itemSubName)
    {
        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                    var items = tlist.FirstOrDefault(p => p.SubName==itemSubName);
                    if (items != null)
                    {
                        VideoSubDetailListRing.IsRunning=true;

                        try
                        {
                            using (HttpClient httpClient = new HttpClient())
                            {
                                if (!string.IsNullOrWhiteSpace(items.UserAgent))
                                {
                                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(items.UserAgent);
                                }
                                HttpResponseMessage response = await httpClient.GetAsync(items.SubURL);

                                int statusCode = (int)response.StatusCode;
                                if (!response.IsSuccessStatusCode)
                                {
                                    await DisplayAlert("��ʾ��Ϣ", "����ʧ�ܣ�" + "HTTP������룺" + statusCode, "ȷ��");
                                    VideoSubDetailListRing.IsRunning=false;
                                    return;
                                }

                                AllVideoData = await response.Content.ReadAsStringAsync();
                                AutoSelectRecommendRegex();
                            }
                        }
                        catch (Exception ex)
                        {
                            await DisplayAlert("��ʾ��Ϣ", "��������ʧ�ܣ����Ժ����ԣ�", "ȷ��");
                            VideoSubDetailListRing.IsRunning=false;
                            return;
                        }

                        VideoSubDetailListRing.IsRunning=false;
                        items.SubDetailStr = AllVideoData;
                        items.SubVDL=CurrentVideoSubDetailList;

                        using (StringWriter sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, tlist);
                            File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                        }

                        await DisplayAlert("��ʾ��Ϣ", "���¶��ĳɹ���", "ȷ��");

                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�", "ȷ��");
                    }
                }
                else
                {
                    await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "��������ʱ������ˢ�����ԣ�", "ȷ��");
            }

        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
        }
    }
    public async Task<string> UpdateAllSubFunc(string itemSubName)
    {
        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                    var items = tlist.FirstOrDefault(p => p.SubName==itemSubName);
                    if (items != null)
                    {                   
                        try
                        {
                            using (HttpClient httpClient = new HttpClient())
                            {
                                if (!string.IsNullOrWhiteSpace(items.UserAgent))
                                {
                                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(items.UserAgent);
                                }
                                HttpResponseMessage response = await httpClient.GetAsync(items.SubURL);

                                int statusCode = (int)response.StatusCode;
                                if (!response.IsSuccessStatusCode)
                                {
                                    return "����ʧ�ܣ�" + "HTTP������룺" + statusCode;
                                }

                                items.SubDetailStr = await response.Content.ReadAsStringAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            return "��������ʧ�ܣ����Ժ����ԣ�";
                        }

                        //items.SubVDL=CurrentVideoSubDetailList;

                        using (StringWriter sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, tlist);
                            File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                        }

                        return "";

                    }
                    else
                    {
                        return "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�";
                    }
                }
                else
                {
                    return "Դ�ļ���ʧ�������´�����";
                }
            }
            catch (Exception)
            {
                return "��������ʱ������ˢ�����ԣ�";
            }

        }
        else
        {
            return "Դ�ļ���ʧ�������´�����";
        }
    }

    public string DeleteSubFunc(string itemSubName)
    {
        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
            try
            {
                if (File.Exists(dataPath+"\\VideoSubList.log"))
                {
                    var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                    var items = tlist.FirstOrDefault(p => p.SubName==itemSubName);
                    if (items != null)
                    {
                        tlist.Remove(items);
                        //����������������ˢ��

                        using (StringWriter sw = new StringWriter())
                        {
                            xmlSerializer.Serialize(sw, tlist);
                            File.WriteAllText(dataPath+"\\VideoSubList.log", sw.ToString());
                        }

                        //return "ɾ�����ĳɹ���";
                        return "";
                    }
                    else
                    {
                        return "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�";
                    }
                }
                else
                {
                    return "Դ�ļ���ʧ�������´�����";
                }
            }
            catch (Exception)
            {
                return "��������ʱ������ˢ�����ԣ�";
            }

        }
        else
        {
            return "Դ�ļ���ʧ�������´�����";
        }
    }

    public async void AutoSelectRecommendRegex()
    {
        if (string.IsNullOrWhiteSpace(AllVideoData))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������", "ȷ��");
            return;
        }

        int tvglogoIndex = AllVideoData.IndexOf("tvg-logo=");
        int tvgnameIndex = AllVideoData.IndexOf("tvg-name=");
        //int OldSelectedIndex = RegexSelectBox.SelectedIndex;

        if (tvglogoIndex>-1&&tvgnameIndex>-1)
        {
            if (tvgnameIndex<tvglogoIndex)
            {
                RegexSelectIndex=1;
                //RegexSelectBox.SelectedIndex=1;
                //RecommendRegexTb.Text = "2";
                RecommendRegex="2";
            }
            else
            {
                RegexSelectIndex=0;
                //RegexSelectBox.SelectedIndex=0;
                //RecommendRegexTb.Text = "1";
                RecommendRegex="1";
            }
        }
        else if (tvglogoIndex>-1)
        {
            RegexSelectIndex=2;
            //RegexSelectBox.SelectedIndex=2;
            //RecommendRegexTb.Text = "3";
            RecommendRegex="3";
        }
        else if (tvglogoIndex<0&&tvgnameIndex<0&&!AllVideoData.Contains("#EXTINF:"))
        {
            RegexSelectIndex=4;
            //RegexSelectBox.SelectedIndex=4;
            //RecommendRegexTb.Text = "5";
            RecommendRegex="5";
        }
        else
        {
            RegexSelectIndex=3;
            //RegexSelectBox.SelectedIndex=3;
            //RecommendRegexTb.Text = "4";
            RecommendRegex="4";
        }


        LoadDataToCheckList(AllVideoData);
        //�ֶ�����
        /*
                 if (OldSelectedIndex==RegexSelectBox.SelectedIndex)
                {
                    LoadDataToCheckList();
                }
         */

    }
    public async void LoadDataToCheckList(string allVideoData)
    {
        if (string.IsNullOrWhiteSpace(allVideoData))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������", "ȷ��");
            return;
        }

        //VideoSubDetailListRing.IsRunning=true;

        RegexManager regexManager = new RegexManager();
        CurrentVideoSubDetailList=regexManager.DoRegex(allVideoData, regexManager.GetRegexOptionIndex(RegexOption1, (RegexSelectIndex+1).ToString()));

        ProcessPageJump(CurrentVideoSubDetailList, 1);
    }

    private async void OpenRegexPageBtn_Clicked(object sender, EventArgs e)
    {
        RegexSelectPopup regexSelectPopup = new RegexSelectPopup(2, RegexSelectIndex, RecommendRegex);
        await this.ShowPopupAsync(regexSelectPopup);

        if (regexSelectPopup.isOKBtnClicked)
        {
            LoadDataToCheckList(AllVideoData);
        }
    }

    private void VideoSubDetailList_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            VideoSubDetailList.SelectedItem=null;

            if (VideoSubDetailList.ItemsSource is not null&&VideoSubDetailList.ItemsSource.Cast<VideoDetailList>().Count()>0)
            {
                VSDLIfmText.IsVisible=false;
                VSDLPagePanel.IsVisible=true;
            }
            else
            {
                VSDLIfmText.IsVisible=true;
                VSDLPagePanel.IsVisible=false;
            }
        }
    }

    private void VSDLBackBtn_Clicked(object sender, EventArgs e)
    {
        if (VSDLCurrentPageIndex<=1)
        {
            return;
        }

        ProcessPageJump(CurrentVideoSubDetailList, VSDLCurrentPageIndex-1);
    }

    private async void VSDLJumpBtn_Clicked(object sender, EventArgs e)
    {
        int TargetPage = 1;
        if (!int.TryParse(VSDLPageTb.Text, out TargetPage))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }
        if (TargetPage<1||TargetPage>VSDLMaxPageIndex)
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }

        ProcessPageJump(CurrentVideoSubDetailList, TargetPage);
    }

    private void VSDLNextBtn_Clicked(object sender, EventArgs e)
    {
        if (VSDLCurrentPageIndex>=VSDLMaxPageIndex)
        {
            return;
        }

        ProcessPageJump(CurrentVideoSubDetailList, VSDLCurrentPageIndex+1);
    }

    /// <summary>
    /// ��תҳ��Ĳ���
    /// </summary>
    /// <param name="videoCheckList">Ҫ�������б�</param>
    /// <param name="TargetPage">Ŀ��ҳ��</param>
    public void ProcessPageJump(List<VideoDetailList> videoCheckList, int TargetPage)
    {
        VSDLMaxPageIndex= (int)Math.Ceiling(videoCheckList.Count/D_VSDL_COUNT_PER_PAGE);

        VSDLCurrentPageIndex=TargetPage;
        VSDLCurrentPage.Text=TargetPage+"/"+VSDLMaxPageIndex;

        MakeVideosDataToPage(videoCheckList, (VSDLCurrentPageIndex-1)*VSDL_COUNT_PER_PAGE);
    }
    public void MakeVideosDataToPage(List<VideoDetailList> list, int skipcount)
    {
        if (list.Count()<1)
        {
            VideoSubDetailList.ItemsSource=new List<VideoDetailList>() { };
            return;
        }

        if (VSDLCurrentPageIndex==VSDLMaxPageIndex)
        {
            VideoSubDetailList.ItemsSource=list.Skip(skipcount).Take(list.Count-skipcount);
        }
        else
        {
            VideoSubDetailList.ItemsSource=list.Skip(skipcount).Take(VSDL_COUNT_PER_PAGE);
        }

        if (VSDLCurrentPageIndex>=VSDLMaxPageIndex)
        {
            VSDLBackBtn.IsEnabled = true;
            VSDLNextBtn.IsEnabled = false;
        }
        else if (VSDLCurrentPageIndex<=1)
        {
            VSDLBackBtn.IsEnabled = false;
            VSDLNextBtn.IsEnabled = true;
        }
        else
        {
            VSDLBackBtn.IsEnabled = true;
            VSDLNextBtn.IsEnabled = true;
        }

    }

    private async void VideoSubList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
       VideoSubList videoSubList=e.Item as VideoSubList;

        if (videoSubList!=null)
        {
            string dataPath = new APPFileManager().GetOrCreateAPPDirectory("AppData\\VideoSubList");
            if (dataPath != null)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<VideoSubList>));
                try
                {
                    if (File.Exists(dataPath+"\\VideoSubList.log"))
                    {
                        var tlist = (List<VideoSubList>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"\\VideoSubList.log")));

                        var items = tlist.FirstOrDefault(p => p.SubName==videoSubList.SubName);
                        if (items != null)
                        {
                            if(string.IsNullOrWhiteSpace(items.SubDetailStr))
                            {
                                await DisplayAlert("��ʾ��Ϣ", "��⵽���ض�������Ϊ�գ��볢�Ը��¶��Ļ�ȡ�������ݣ�", "ȷ��");
                                return;
                            }

                            AllVideoData = items.SubDetailStr;
                            AutoSelectRecommendRegex();

                        }
                        else
                        {
                            await DisplayAlert("��ʾ��Ϣ", "δ���ҵ���ǰ�������ƶ�Ӧ�ı������ݣ������ԣ�", "ȷ��");
                        }

                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
                    }
                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ȡ��������ʱ������ˢ�����ԣ�����¶��ģ�", "ȷ��");
                }

            }
            else
            {
                await DisplayAlert("��ʾ��Ϣ", "Դ�ļ���ʧ�������´�����", "ȷ��");
            }
        }
       
    }

    private async void VideoSubListUpdateAllBtn_Clicked(object sender, EventArgs e)
    {
        var videoSubList = VideoSubList.ItemsSource;
        if (videoSubList is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ�", "ȷ��");
            return;
        }

        var tlist = videoSubList.Cast<VideoSubList>().Where(p => p.IsEnabledUpdate).ToList();
        if (tlist.Count < 1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�����б���û�����ø��µĶ��ģ�", "ȷ��");
            return;
        }
        
        VideoSubListUpdateAllBtn.IsEnabled=false;
        VideoSubDetailListRing.IsRunning=true;

        await Task.Run(async() =>
        {
            for (int i = 0; i < tlist.Count; i++)
            {
                await UpdateAllSubFunc(tlist[i].SubName);
            }

        }).ContinueWith(async (p) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                //�����ǰ�Ҳ��б���չʾ�����������ѡ�еĶ��ģ���ִ�и����Ҳ��UI��
                var selectItem = VideoSubList.SelectedItem;
                if (selectItem != null)
                {
                    AllVideoData = (selectItem as VideoSubList).SubDetailStr;
                    AutoSelectRecommendRegex();
                }

                VideoSubListUpdateAllBtn.IsEnabled=true;
                VideoSubDetailListRing.IsRunning=false;
                await DisplayAlert("��ʾ��Ϣ", "��ִ�и���"+tlist.Count+"�����ģ�", "ȷ��");
            });
        });

    }

    private async void VideoSubDetailList_ItemTapped(object sender, ItemTappedEventArgs e)
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


        var mainpage = (Shell)App.Current.Windows[0].Page;
        mainpage.CurrentItem = mainpage.Items.FirstOrDefault();
        await mainpage.Navigation.PopToRootAsync();
    }

    private async void VSLRemoveCheckedBtn_Clicked(object sender, EventArgs e)
    {
        if (VideoSubList.ItemsSource is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ�", "ȷ��");
            return;
        }

        var tlist = VideoSubList.ItemsSource.Cast<VideoSubList>().Where(p => p.IsSelected).ToList();
        if (tlist.Count < 1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�����б���û�б���ѡ�Ķ��ģ�", "ȷ��");
            return;
        }
        if (!await DisplayAlert("��ʾ��Ϣ", "��Ҫ����ɾ��"+tlist.Count+"��������", "ȷ��", "ȡ��"))
        {
            return;
        }

        await Task.Run(() =>
        {
            for (int i = 0; i<tlist.Count; i++)
            {
                DeleteSubFunc(tlist[i].SubName);
            }

        }).ContinueWith(async (p) =>
        {
            ReadLocalSubList();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("��ʾ��Ϣ", "��ִ���Ƴ�"+tlist.Count+"�����ݣ�", "ȷ��");
            });
        });

        /*
                 var tlist=VideoSubList.ItemsSource.Cast<VideoSubList>().ToList();
                var tcount = tlist.Where(p => p.IsSelected).Count();
                if (tcount < 1)
                {
                    await DisplayAlert("��ʾ��Ϣ", "��ǰ�����б���û�б���ѡ�Ķ��ģ�", "ȷ��");
                    return;
                }

                int successRemoveCount = 0;
                await Task.Run(() =>
                {
                    for (int i = tlist.Count-1; i>=0; i--)
                    {
                        if (tlist[i].IsSelected)
                        {
                            if(DeleteSubFunc(tlist[i].SubName)=="")
                            {
                                successRemoveCount++;
                            }
                            tlist.RemoveAt(i);
                        }
                    }

                }).ContinueWith(async(p)=>
                {
                    RefreshVSL(tlist);
                    await DisplayAlert("��ʾ��Ϣ", "�ѳɹ��Ƴ�"+successRemoveCount+"�����ݣ�", "ȷ��");
                },TaskScheduler.FromCurrentSynchronizationContext());
         */
    }

    private async void VideoSubListSelectCB_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (VideoSubList.ItemsSource is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б�Ϊ�գ�", "ȷ��");
            return;
        }

        var tlist = VideoSubList.ItemsSource.Cast<VideoSubList>().ToList();   
        
        tlist.ForEach(p =>
        {
            p.IsSelected=e.Value;
        });

        VideoSubList.ItemsSource=tlist;
    }
}