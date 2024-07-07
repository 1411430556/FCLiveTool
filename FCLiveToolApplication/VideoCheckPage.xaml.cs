using CommunityToolkit.Maui.Storage;

namespace FCLiveToolApplication;

public partial class VideoCheckPage : ContentPage
{
    public VideoCheckPage()
    {
        InitializeComponent();
    }
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        InitRegexList();
    }
    public void InitRegexList()
    {
        List<string> RegexOption = new List<string>() { "����1", "����2", "����3", "����4", "����5" };
        RegexSelectBox.ItemsSource = RegexOption;
        RegexSelectBox.SelectedIndex=2;
    }

    List<VideoDetailList> CurrentCheckList = new List<VideoDetailList>();
    string AllVideoData;
    public int CheckFinishCount = 0;
    public int CheckOKCount = 0;
    public int CheckNOKCount = 0;
    CancellationTokenSource M3U8ValidCheckCTS;
    public const string DEFAULT_USER_AGENT = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36";
    public bool ShowLoadOrRefreshDialog = false;
    public bool isFinishCheck = false;

    private void M3USourceRBtn_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        RadioButton entry = sender as RadioButton;

        if (entry.StyleId == "M3USourceRBtn1")
        {
            LocalM3USelectPanel.IsVisible = true;
            M3USourcePanel.IsVisible = false;
        }
        else if (entry.StyleId == "M3USourceRBtn2")
        {
            LocalM3USelectPanel.IsVisible = false;
            M3USourcePanel.IsVisible = true;
        }
    }

    private async void SelectLocalM3UFileBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult != 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
{
    { DevicePlatform.iOS, new[] { "com.apple.mpegurl" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.macOS, new[] {  "application/vnd.apple.mpegurl" } },
    { DevicePlatform.Android, new[] { "audio/x-mpegurl"  } },
    { DevicePlatform.WinUI, new[] { ".m3u"} }
});

        var filePicker = await FilePicker.PickAsync(new PickOptions()
        {
            PickerTitle = "ѡ��M3U�ļ�",
            FileTypes=fileTypes
        });

        if (filePicker is not null)
        {
            LocalMFileTb.Text=filePicker.FullPath;
            AllVideoData =File.ReadAllText(filePicker.FullPath);

            LoadDataToCheckList();
        }
        else
        {
            LocalMFileTb.Text = "��ȡ��ѡ��";
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }

    }
    public async void LoadDataToCheckList()
    {
        if (string.IsNullOrWhiteSpace(AllVideoData))
        {
            await DisplayAlert("��ʾ��Ϣ", "ʲô��û��ȡ������������Դ��", "ȷ��");
            return;
        }
        if (AllVideoData.Contains("tvg-name="))
        {
            RecommendRegexTb.Text = "1��2";
        }
        else if (AllVideoData.Contains("tvg-logo=")&&!AllVideoData.Contains("tvg-name="))
        {
            RecommendRegexTb.Text = "3��4";
        }
        else
        {
            RecommendRegexTb.Text = "-";
        }

        ClearAllCount();

        RegexManager regexManager = new RegexManager();
        CurrentCheckList=regexManager.DoRegex(AllVideoData, regexManager.GetRegexOptionIndex(RegexOptionCB.IsChecked, (RegexSelectBox.SelectedIndex+1).ToString()));
       
        CheckProgressText.Text="0 / "+CurrentCheckList.Count;
        VideoCheckList.ItemsSource= CurrentCheckList;
    }
    private async void M3UAnalysisBtn_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(M3USourceURLTb.Text))
        {
            await DisplayAlert("��ʾ��Ϣ", "������ֱ��ԴM3U8��ַ��", "ȷ��");
            return;
        }
        if (!M3USourceURLTb.Text.Contains("://"))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ݲ�����URL�淶��", "ȷ��");
            return;
        }


        string[] options = new string[2];
        using (Stream stream = await new VideoManager().DownloadM3U8FileToStream(M3USourceURLTb.Text, options))
        {
            if (stream is null)
            {
                await DisplayAlert("��ʾ��Ϣ", options[0], "ȷ��");
                return;
            }

            string result = "";
            using (StreamReader sr = new StreamReader(stream))
            {
                string r = "";
                while ((r = await sr.ReadLineAsync()) != null)
                {
                    result+=r+"\n";
                }
            }

            AllVideoData=result;
            LoadDataToCheckList();
        }
    }

    private async void StartCheckBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentCheckList.Count<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û��ֱ��Դ���볢�Ը���һ������������", "ȷ��");
            return;
        }

        ClearAllCount();
        M3U8ValidCheckCTS=new CancellationTokenSource();
        StopCheckBtn.IsEnabled=true;
        CheckDataSourcePanel.IsEnabled=false;
        ShowLoadOrRefreshDialog=false;
        isFinishCheck = false;

        ValidCheck(CurrentCheckList);
        while (CheckFinishCount<CurrentCheckList.Count)
        {
            if (M3U8ValidCheckCTS.IsCancellationRequested)
            {
                break;
            }
            await Task.Delay(1000);
        }

        isFinishCheck=true;
        StopCheckBtn.IsEnabled=false;
        CheckDataSourcePanel.IsEnabled = true;
        RemoveNOKBtn.IsEnabled=true;

        if (!M3U8ValidCheckCTS.IsCancellationRequested)
        {
            VideoCheckList.ItemsSource=CurrentCheckList.Take(CurrentCheckList.Count);

            await DisplayAlert("��ʾ��Ϣ", "��ȫ�������ɣ�", "ȷ��");
        }
        else
        {
            if (ShowLoadOrRefreshDialog)
            {
                bool tresult = await DisplayAlert("��ʾ��Ϣ", "��Ҫ�鿴���ּ����Ľ������Ҫ���¼����б�", "�鿴���", "���¼���");
                if (tresult)
                {
                    VideoCheckList.ItemsSource=CurrentCheckList.Take(CurrentCheckList.Count);
                }
                else
                {
                    LoadDataToCheckList();
                }
            }
            else
            {
                LoadDataToCheckList();
                await DisplayAlert("��ʾ��Ϣ", "����ȡ����⣡", "ȷ��");
            }

        }

    }
    public void ClearAllCount()
    {
        //CheckProgressText.Text="0 / "+CurrentCheckList.Count;
        CheckOKCountText.Text="0";
        CheckNOKCountText.Text="0";
        CheckNOKErrorCodeList.ItemsSource=null;
        CheckFinishCount = 0;
        CheckOKCount = 0;
        CheckNOKCount = 0;
    }
    private void VideoCheckList_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            VideoCheckList.SelectedItem=null;

            if (VideoCheckList.ItemsSource is not null&&VideoCheckList.ItemsSource.Cast<VideoDetailList>().Count()>0)
            {
                VCLIfmText.IsVisible=false;
            }
            else
            {
                VCLIfmText.IsVisible=true;
            }
        }
    }
    public async void ValidCheck(List<VideoDetailList> videodetaillist)
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

                            lock (obj)
                            {
                                CheckOKCount++;

                                MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    CheckOKCountText.Text=CheckOKCount.ToString();
                                });
                            }
                        }
                        else
                        {
                            vd.HTTPStatusCode=statusCode.ToString();
                            vd.HTTPStatusTextBKG=Colors.Orange;

                            lock (obj)
                            {
                                CheckNOKCount++;

                                MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    CheckNOKCountText.Text=CheckNOKCount.ToString();
                                });
                            }
                        }

                    }
                    catch (OperationCanceledException)
                    {
                        //�ֶ�ֹͣ��δ���м��ģ���ʱ��ͳ��
                        if (!M3U8ValidCheckCTS.IsCancellationRequested)
                        {
                            vd.HTTPStatusCode="Timeout";
                            vd.HTTPStatusTextBKG=Colors.Purple;

                            lock (obj)
                            {
                                CheckNOKCount++;

                                MainThread.InvokeOnMainThreadAsync(() =>
                                {
                                    CheckNOKCountText.Text=CheckNOKCount.ToString();
                                });
                            }

                        }

                    }
                    catch (Exception)
                    {
                        vd.HTTPStatusCode="ERROR";
                        vd.HTTPStatusTextBKG=Colors.Red;

                        lock (obj)
                        {
                            CheckNOKCount++;

                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                CheckNOKCountText.Text=CheckNOKCount.ToString();
                            });
                        }
                    }

                    if (!M3U8ValidCheckCTS.IsCancellationRequested)
                    {
                        lock (obj)
                        {
                            CheckFinishCount++;

                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                CheckProgressText.Text=CheckFinishCount+" / "+videodetaillist.Count;
                            });
                        }


                    }

                }
            });
            thread.Start();

            await Task.Delay(10);
        }
    }
    private void RegexSelectTipBtn_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("������Ϣ", new MsgManager().GetRegexOptionTip(), "�ر�");
    }

    private void RegexSelectBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AllVideoData))
        {
            return;
        }

        LoadDataToCheckList();
    }

    private async void StopCheckBtn_Clicked(object sender, EventArgs e)
    {
        if (M3U8ValidCheckCTS!=null)
        {
            bool CancelCheck = await DisplayAlert("��ʾ��Ϣ", "��Ҫֹͣ�����ֹͣ����ʱ��֧�ָֻ����ȡ�", "ȷ��", "ȡ��");
            if (CancelCheck)
            {
                M3U8ValidCheckCTS.Cancel();
                //������ע�͵�
                StopCheckBtn.IsEnabled=false;
                CheckDataSourcePanel.IsEnabled=true;
                ShowLoadOrRefreshDialog = true;
            }

        }
    }

    private async void RemoveNOKBtn_Clicked(object sender, EventArgs e)
    {
        if (CurrentCheckList.Count<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û��ֱ��Դ���볢�Ը���һ������������", "ȷ��");
            return;
        }
        if (!isFinishCheck)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ����ִ��ֱ��Դ��⣡", "ȷ��");
            return;
        }
        var notokcount = CurrentCheckList.Where(p => (p.HTTPStatusCode!="OK")&&(p.HTTPStatusCode!=null)).Count();
        if (notokcount<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û����Ч��ֱ���źţ����������", "ȷ��");
            return;
        }


        for (int i = CurrentCheckList.Count-1; i >=0; i--)
        {
            if (CurrentCheckList[i].HTTPStatusCode!="OK"&&CurrentCheckList[i].HTTPStatusCode!=null)
            {
                AllVideoData=AllVideoData.Replace(CurrentCheckList[i].FullM3U8Str, "");
                CurrentCheckList.RemoveAt(i);
            }
        }

        VideoCheckList.ItemsSource = CurrentCheckList.Take(CurrentCheckList.Count);
        await DisplayAlert("��ʾ��Ϣ", "�ѳɹ����б����Ƴ����й�"+notokcount+"����Ч��ֱ���źţ�", "ȷ��");
    }
}