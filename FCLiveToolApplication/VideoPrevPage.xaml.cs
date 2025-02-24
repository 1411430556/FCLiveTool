using CommunityToolkit.Maui.Storage;
using System.Xml.Serialization;

namespace FCLiveToolApplication;

public partial class VideoPrevPage : ContentPage
{
    public VideoPrevPage()
    {
        InitializeComponent();
    }
    public static VideoPrevPage videoPrevPage;
    public List<string[]> M3U8PlayList = new List<string[]>();
    public List<LocalM3U8List> CurrentLocalM3U8List = new List<LocalM3U8List>();
    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (videoPrevPage != null)
        {
            return;
        }
        videoPrevPage=this;

        NowPlayingTb.Text =Preferences.Get("DefaultPlayM3U8Name", "");
        VideoWindow.Source= Preferences.Get("DefaultPlayM3U8URL", "");
        //VideoWindow.ShouldAutoPlay=Preferences.Get("StartAutoPlayM3U8", true);
        if (Preferences.Get("StartAutoPlayM3U8", true))
        {
            VideoWindow.Play();
        }


        ReadAndLoadLocalM3U8();
        await Task.Delay(1000);
        LoadRecent();
#if ANDROID
        RecentPanel.WidthRequest=PageGrid.Width;
#endif

        //CurrentURL=(VideoWindow.Source as UriMediaSource).Uri.ToString();
    }
    private void PageGrid_SizeChanged(object sender, EventArgs e)
    {
#if WINDOWS

        VideoWindow.WidthRequest=PageGrid.Width;
        VideoWindow.HeightRequest=PageGrid.Height-50;

        RecentList.HeightRequest=PageGrid.Height-100;
        LocalM3U8Panel.HeightRequest=PageGrid.Height-50;
        //LocalM3U8List.HeightRequest=PageGrid.Height-(LocalM3U8SP.Height+100);

        //���������ŵ��б���չ��״̬���򴰿ڴ�С�ı�ʱҲҪͬʱ����������֤������λ
        if (RecentPanel.Width>0)
        {
            ShowRecentAnimation(true);
        }
#endif
#if ANDROID
        //��׿���� 
        if (DeviceDisplay.Current.MainDisplayInfo.Orientation==DisplayOrientation.Landscape)
        {
            CheckRecentBtn.IsVisible=true;

            PageGrid.SetRow(RecentPanel, 1);

            RecentPanel.IsVisible=false;
            RecentPanel.HeightRequest=PageGrid.Height-50;

            VideoWindow.HeightRequest=PageGrid.Height-50;

            LocalM3U8List.HeightRequest=VideoWindow.HeightRequest/3;
        }
        //��׿����
        else
        {
            CheckRecentBtn.IsVisible=false;

            PageGrid.SetRow(RecentPanel, 2);

            RecentPanel.IsVisible=true;
            RecentPanel.ClearValue(HeightRequestProperty);
            RecentPanel.ClearValue(WidthRequestProperty);

            VideoWindow.HeightRequest=PageGrid.Width;

            LocalM3U8List.HeightRequest=VideoWindow.HeightRequest/3;
        }
#endif
    }
    public async void ReadAndLoadLocalM3U8()
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ����Ͷ�ȡ�ļ����������Ȩ�������漰�ļ���д�Ĳ������޷�����ʹ�ã�", "ȷ��");
            return;
        }

        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("LocalM3U8Log");
        if (dataPath != null)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<LocalM3U8List>));
            try
            {
                if (File.Exists(dataPath+"/LocalM3U8.log"))
                {
                    var tlist = (List<LocalM3U8List>)xmlSerializer.Deserialize(new StringReader(File.ReadAllText(dataPath+"/LocalM3U8.log")));

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LocalM3U8List.ItemsSource = tlist;
                    });
                }
            }
            catch (Exception)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ȡ����M3U8�����б�ʱ����", "ȷ��");
            }

        }

    }

    private async void CheckRecentBtn_Clicked(object sender, EventArgs e)
    {
#if WINDOWS
        if (RecentPanel.Width==0)
        {
            ShowRecentAnimation(true);
        }
        else
        {
            ShowRecentAnimation(false);
        }
#endif
        //��׿����ʱ����ʾ��ǰ��ť�������¼�
#if ANDROID
        if (RecentPanel.IsVisible)
        {
            ShowRecentAnimation(false);
            await Task.Delay(1000);
            RecentPanel.IsVisible=false;
        }
        else
        {
            ShowRecentAnimation(true);
            RecentPanel.IsVisible=true;
        }
#endif
    }
    private async void RecentList_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        List<string[]> tmlist = new List<string[]>();
        M3U8PlayList.ForEach(tmlist.Add);


        var selectitem = e.Item as RecentVList;

        string readresult = await new VideoManager().DownloadAndReadM3U8File(M3U8PlayList, new string[] { selectitem.SourceName, selectitem.SourceLink });
        if (readresult!="")
        {
            M3U8PlayList=tmlist;
            await DisplayAlert("��ʾ��Ϣ", readresult, "ȷ��");
            return;
        }


        M3U8PlayList.Insert(0, new string[] { "Ĭ��", selectitem.SourceLink });
        string[] MOptions = new string[M3U8PlayList.Count];
        MOptions[0]="Ĭ��\n";
        string WantPlayURL = selectitem.SourceLink;

        if (M3U8PlayList.Count > 2)
        {
            for (int i = 1; i<M3U8PlayList.Count; i++)
            {
                MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
            }

            string MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
            if (MSelectResult == "ȡ��"||MSelectResult is null)
            {
                M3U8PlayList=tmlist;
                return;
            }
            else if (!MSelectResult.Contains("Ĭ��"))
            {
                int tmindex = Convert.ToInt32(MSelectResult.Remove(0, 1).Split("��")[0]);
                WantPlayURL=M3U8PlayList[tmindex][1];
            }

        }


        VideoWindow.Source=WantPlayURL;
        VideoWindow.Play();
        NowPlayingTb.Text=selectitem.SourceName;
    }
    public async void LoadRecent()
    {
        RecentListRing.IsRunning=true;
        //δ���سɹ����������ݣ��Կɲ���ԭ��������
        try
        {
            string videodata = await new HttpClient().GetStringAsync("https://fclivetool.com/api/GetRecent");
            videodata= videodata.Replace("/img/TVSICON.png", "fclive_tvicon.png");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<RecentVList>));
            List<RecentVList> list = (List<RecentVList>)xmlSerializer.Deserialize(new StringReader(videodata));
            list.ForEach(p => p.PastTime=GetPastTime(p.AddDT));

            RecentList.ItemsSource =list;
        }
        catch (Exception ex)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ȡ�����������ʧ�ܣ����Ժ����ԣ�", "ȷ��");
        }

        RecentListRing.IsRunning=false;
    }
    public string GetPastTime(DateTime dt)
    {
        if (dt.Year==0001||dt.Year==1970)
        {
            return "";
        }

        TimeSpan PastTime = DateTime.UtcNow.AddHours(8)-dt;
        if (PastTime<TimeSpan.FromMinutes(1))
        {
            return PastTime.Seconds+"����ǰ";
        }
        else if (PastTime<TimeSpan.FromHours(1))
        {
            return PastTime.Minutes+"����ǰ";
        }
        else if (PastTime<TimeSpan.FromDays(1))
        {
            return PastTime.Hours+"Сʱǰ";
        }
        else if (PastTime<TimeSpan.FromDays(30))
        {
            return PastTime.Days+"��ǰ";
        }
        else if (PastTime==TimeSpan.FromDays(30))
        {
            return "һ����ǰ";
        }
        else
        {
            return "����һ����";
        }

    }

    public void ShowRecentAnimation(bool NeedOpen)
    {
        //չ��
        if (NeedOpen)
        {
            var animation = new Animation(v => RecentPanel.WidthRequest = v, 0, PageGrid.Width);
            animation.Commit(this, "ShowRec", 16, 500, Easing.BounceOut);
        }
        //�ջ�
        else
        {
            var animation = new Animation(v => RecentPanel.WidthRequest = v, RecentPanel.Width, 0);
            animation.Commit(this, "HiddenRec", 16, 500, Easing.CubicInOut);
        }
    }

    private void RecentList_Refreshing(object sender, EventArgs e)
    {       
        //��ʹ��ListView�Լ��ļ���Ȧ
        RecentList.IsRefreshing=false;

        LoadRecent();
    }

    private void RLRefreshBtn_Clicked(object sender, EventArgs e)
    {
        LoadRecent();
    }

    private async void PlaylistBtn_Clicked(object sender, EventArgs e)
    {
        string MSelectResult;

        if (M3U8PlayList.Count<=2)
        {
            MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, new string[] { "Ĭ��\n" });
            if (MSelectResult == "ȡ��"||MSelectResult is null)
            {
                return;
            }
            else
            {
                VideoWindow.Stop();
                VideoWindow.Source=VideoWindow.Source;
            }
        }
        else
        {
            string[] MOptions = new string[M3U8PlayList.Count];
            MOptions[0]="Ĭ��\n";
            for (int i = 1; i<M3U8PlayList.Count; i++)
            {
                MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
            }

            MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
            if (MSelectResult == "ȡ��"||MSelectResult is null)
            {
                return;
            }
            else if (!MSelectResult.Contains("Ĭ��"))
            {
                int tmindex = Convert.ToInt32(MSelectResult.Remove(0, 1).Split("��")[0]);
                VideoWindow.Source=M3U8PlayList[tmindex][1];
            }
            else
            {
                VideoWindow.Stop();
                VideoWindow.Source=M3U8PlayList[0][1];
            }
        }

        VideoWindow.Play();
    }

    private void LocalM3U8Btn_Clicked(object sender, EventArgs e)
    {
        //�ջ�
        if (LocalM3U8Panel.TranslationY==0)
        {
            var animation = new Animation {
                { 0, 1, new Animation(v => LocalM3U8Panel.TranslationY  = v, 0, -1000) },
                { 0, 1, new Animation(v => LocalM3U8Panel.Opacity  = v, 1, 0) }
            };
            animation.Commit(this, "MPanelAnimation", 16, 500, Easing.CubicInOut);

        }
        //չ��
        else
        {
            var animation = new Animation {
                { 0, 1,  new Animation(v => LocalM3U8Panel.TranslationY  = v, -1000, 0)},
                { 0, 1, new Animation(v => LocalM3U8Panel.Opacity  = v, 0, 1) }
            };
            animation.Commit(this, "MPanelAnimation", 16, 500, Easing.CubicOut);

        }
    }

    private async void SelectLocalM3U8FolderBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        //ÿ�ε����ť����ǰ�б����ݷ������������ʱ���жԱ�
        if (LocalM3U8List.ItemsSource is not null)
        {
            CurrentLocalM3U8List=LocalM3U8List.ItemsSource.Cast<LocalM3U8List>().ToList();
        }
        //VideoWindow.Source=new Uri("C:\\Users\\Lee\\Desktop\\cgtn-f.m3u8");
        var folderPicker = await FolderPicker.PickAsync(FileSystem.AppDataDirectory, CancellationToken.None);

        if (folderPicker.IsSuccessful)
        {
            List<LocalM3U8List> mlist = new List<LocalM3U8List>();
            LoadM3U8FileFromSystem(folderPicker.Folder.Path, ref mlist);
            CurrentLocalM3U8List.AddRange(mlist);

            //����������
            int tmindex = 0;          
            CurrentLocalM3U8List.ForEach(p =>
            {
                p.ItemId="LMRB"+tmindex;
                tmindex++;
            });
            LocalM3U8List.ItemsSource=CurrentLocalM3U8List;
        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }

    }
    private async void SelectLocalM3U8FileBtn_Clicked(object sender, EventArgs e)
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ��ȡ�ļ���", "ȷ��");
            return;
        }

        //ÿ�ε����ť����ǰ�б����ݷ������������ʱ���жԱ�
        if (LocalM3U8List.ItemsSource is not null)
        {
            CurrentLocalM3U8List=LocalM3U8List.ItemsSource.Cast<LocalM3U8List>().ToList();
        }

        var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
{
    { DevicePlatform.iOS, new[] { "com.apple.mpegurl", "public.m3u8-playlist" , "application/vnd.apple.mpegurl" } },
    { DevicePlatform.macOS, new[] { "public.m3u8", "application/vnd.apple.mpegurl" } },
    { DevicePlatform.Android, new[] {  "audio/x-mpegurl" } },
    { DevicePlatform.WinUI, new[] { ".m3u8" ,".m3u"} }
});
        var filePicker = await FilePicker.PickMultipleAsync(new PickOptions()
        {
            PickerTitle="ѡ��M3U8�ļ�",
            FileTypes=fileTypes
        });

        if (filePicker is not null&&filePicker.Count()>0)
        {
            filePicker.ToList().ForEach(p =>
            {
                if (CurrentLocalM3U8List.Where(p2 => p2.FullFilePath==p.FullPath).Count()<1)
                {
                    CurrentLocalM3U8List.Add(new LocalM3U8List() { FileName=p.FileName, FilePath=p.FullPath.Replace(p.FileName, ""), FullFilePath=p.FullPath });
                }
            });

            //����������
            int tmindex = 0;
            CurrentLocalM3U8List.ForEach(p =>
            {
                p.ItemId="LMRB"+tmindex;
                tmindex++;
            });
            LocalM3U8List.ItemsSource=CurrentLocalM3U8List;
        }
        else
        {
            await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
        }

    }
    public void LoadM3U8FileFromSystem(string path, ref List<LocalM3U8List> list)
    {
        foreach (string item in Directory.EnumerateFileSystemEntries(path).ToList())
        {
            if (File.GetAttributes(item).HasFlag(FileAttributes.Directory))
            {
                LoadM3U8FileFromSystem(item, ref list);
            }
            else
            {
                if (item.ToLower().EndsWith(".m3u8"))
                {
                    string tname;
#if ANDROID
                    tname = item.Substring(item.LastIndexOf("/")+1);
#else
tname = item.Substring(item.LastIndexOf("\\")+1);
#endif
                    //string tfoldername = "."+item.Replace(initFoldername, "").Replace(tname, "");
                    string tfoldername = item.Replace(tname, "");

                    if (CurrentLocalM3U8List.Where(p => p.FullFilePath==item).Count()<1)
                    {
                        list.Add(new LocalM3U8List() { FileName=tname, FilePath=tfoldername, FullFilePath=item });
                    }

                }

            }

        }
    }
    private async void LocalM3U8List_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        LocalM3U8List list = e.Item as LocalM3U8List;

        //��ʱ��дһЩ�������
        List<string[]> tmlist = new List<string[]>();
        M3U8PlayList.ForEach(tmlist.Add);

        string readresult = await new VideoManager().ReadLocalM3U8File(M3U8PlayList, new string[] { list.FileName, list.FullFilePath });
        if (readresult!="")
        {
            M3U8PlayList=tmlist;
            await DisplayAlert("��ʾ��Ϣ", readresult, "ȷ��");
            return;
        }


        M3U8PlayList.Insert(0, new string[] { "Ĭ��", list.FullFilePath });
        string[] MOptions = new string[M3U8PlayList.Count];
        MOptions[0]="Ĭ��\n";
        string WantPlayURL = list.FullFilePath;

        if (M3U8PlayList.Count > 2)
        {
            for (int i = 1; i<M3U8PlayList.Count; i++)
            {
                MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
            }

            string MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
            if (MSelectResult == "ȡ��"||MSelectResult is null)
            {
                M3U8PlayList=tmlist;
                return;
            }
            else if (!MSelectResult.Contains("Ĭ��"))
            {
                int tmindex = Convert.ToInt32(MSelectResult.Remove(0, 1).Split("��")[0]);
                WantPlayURL=M3U8PlayList[tmindex][1];
            }

            if (WantPlayURL=="")
            {
                await DisplayAlert("��ʾ��Ϣ", "��ǰֱ��Դ��URL����Ե�ַ�����Ǿ��Ե�ַ����Ϊ��֪��������ַ�����޷����Ÿ�ֱ��Դ��", "ȷ��");
                return;
            }
        }


        VideoWindow.Source=new Uri(WantPlayURL);
        VideoWindow.Play();
        NowPlayingTb.Text="�����ļ��� "+list.FileName;


    }

    private void LocalM3U8List_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ItemsSource")
        {
            if (LocalM3U8List.ItemsSource is not null&&LocalM3U8List.ItemsSource.Cast<LocalM3U8List>().Count()>0)
            {
                LocalM3U8IfmText.Text="";
            }
            else
            {
                LocalM3U8IfmText.Text="��ǰ�����б���û��ֱ��Դ����ȥ��Ӱ�~";
            }
        }
    }

    private async void LocalM3U8RemoveBtn_Clicked(object sender, EventArgs e)
    {
        Button LMRBtn = sender as Button;
        List<LocalM3U8List> tlist = LocalM3U8List.ItemsSource.Cast<LocalM3U8List>().ToList();

        var item = tlist.Where(p => p.ItemId==LMRBtn.CommandParameter.ToString()).FirstOrDefault();
        if (item is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�Ƴ�ʱ�����쳣", "ȷ��");
            return;
        }
        tlist.Remove(item);

        LocalM3U8List.ItemsSource=tlist;
    }

    private async void SaveLocalM3U8Btn_Clicked(object sender, EventArgs e)
    {
        var tlist = LocalM3U8List.ItemsSource;
        if (tlist is null||tlist.Cast<LocalM3U8List>().Count()<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ǰ�б���û�б���ֱ��Դ�ļ�������ѡ��һЩֱ��Դ��", "ȷ��");
            return;
        }
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ�����ļ���", "ȷ��");
            return;
        }

        string dataPath = new APPFileManager().GetOrCreateAPPDirectory("LocalM3U8Log");
        if (dataPath is null)
        {
            await DisplayAlert("��ʾ��Ϣ", "�����ļ�ʧ�ܣ�������û��Ȩ�޻��ߵ�ǰƽ̨�ݲ�֧�ֱ��������", "ȷ��");
            return;
        }

        using (StringWriter sw = new StringWriter())
        {
            new XmlSerializer(typeof(List<LocalM3U8List>)).Serialize(sw, tlist.Cast<LocalM3U8List>().ToList());
            File.WriteAllText(dataPath+"LocalM3U8.log", sw.ToString());
        }

        await DisplayAlert("��ʾ��Ϣ", "���沥���б�ɹ�����", "ȷ��");
    }

    private void NowPlayingTb_PointerEntered(object sender, PointerEventArgs e)
    {
        NowPlayingTb.Background=Colors.LightYellow;      
    }

    private void NowPlayingTb_PointerExited(object sender, PointerEventArgs e)
    {
        NowPlayingTb.Background=Colors.Transparent;
    }

    private void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {
        ChangeCurrentPlayerSource();
    }
    //Ϊ��Ӧ��PointerGestureRecognizer�ڰ�׿�ϲ������õķ�������΢������޸�
    private void NowPlayingTb_Tapped(object sender, TappedEventArgs e)
    {
#if ANDROID
        ChangeCurrentPlayerSource();
#endif
    }
    public async void ChangeCurrentPlayerSource()
    {
        string urlnewvalue = await DisplayPromptAsync("����һ��ֱ��Դ", "������ֱ��ԴURL��", "����", "ȡ��", "URL...", -1, Keyboard.Text, "");
        if (string.IsNullOrWhiteSpace(urlnewvalue))
        {
            if (urlnewvalue!=null)
                await DisplayAlert("��ʾ��Ϣ", "��������ȷ�����ݣ�", "ȷ��");
            return;
        }
        if (!urlnewvalue.Contains("://"))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ݲ�����URL�淶��", "ȷ��");
            return;
        }


        List<string[]> tmlist = new List<string[]>();
        M3U8PlayList.ForEach(tmlist.Add);

        string readresult = await new VideoManager().DownloadAndReadM3U8File(M3U8PlayList, new string[] { "����ֱ��Դ", urlnewvalue });
        if (readresult!="")
        {
            M3U8PlayList=tmlist;
            await DisplayAlert("��ʾ��Ϣ", readresult, "ȷ��");
            return;
        }


        M3U8PlayList.Insert(0, new string[] { "Ĭ��", urlnewvalue });
        string[] MOptions = new string[M3U8PlayList.Count];
        MOptions[0]="Ĭ��\n";
        string WantPlayURL = urlnewvalue;

        if (M3U8PlayList.Count > 2)
        {
            for (int i = 1; i<M3U8PlayList.Count; i++)
            {
                MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
            }

            string MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
            if (MSelectResult == "ȡ��"||MSelectResult is null)
            {
                M3U8PlayList=tmlist;
                return;
            }
            else if (!MSelectResult.Contains("Ĭ��"))
            {
                int tmindex = Convert.ToInt32(MSelectResult.Remove(0, 1).Split("��")[0]);
                WantPlayURL=M3U8PlayList[tmindex][1];
            }

        }


        VideoWindow.Source=WantPlayURL;
        VideoWindow.Play();
        NowPlayingTb.Text="����ֱ��Դ";
    }
}