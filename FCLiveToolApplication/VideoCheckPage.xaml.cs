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

    List<VideoDetailList> CurrentCheckList=new List<VideoDetailList>();
    string AllVideoData;
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
        if(AllVideoData.Contains("tvg-name="))
        {
           RecommendRegexTb.Text = "1��2";
        }      
        else
        {
            RecommendRegexTb.Text = "-";
        }

        RegexManager regexManager = new RegexManager();
        CurrentCheckList=regexManager.DoRegex(AllVideoData, regexManager.GetRegexOptionIndex(RegexOptionCB.IsChecked, (RegexSelectBox.SelectedIndex+1).ToString()));
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

    private void StartCheckBtn_Clicked(object sender, EventArgs e)
    {

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

    private void RegexSelectTipBtn_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("������Ϣ", new MsgManager().GetRegexOptionTip(), "�ر�");
    }

    private void RegexSelectBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(string.IsNullOrWhiteSpace(AllVideoData))
        {
            return;
        }

        LoadDataToCheckList();
    }
}