using CommunityToolkit.Maui.Storage;
using System.Text;
using System.Xml.Serialization;

namespace FCLiveToolApplication;

public partial class VideoSharePage : ContentPage
{
	public VideoSharePage()
	{
		InitializeComponent();
	}
    public static VideoSharePage videoSharePage;
    public int VSLCurrentPageIndex = 1;
    public List<string[]> M3U8PlayList = new List<string[]>();
    private async void M3UAnalysisBtn_Clicked(object sender, EventArgs e)
    {
        if(string.IsNullOrWhiteSpace(M3USourceURLTb.Text)||!M3USourceURLTb.Text.Contains("://"))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ֱ��Դ��ַ��", "ȷ��");
            return;
        }
        if (!M3USourceURLTb.Text.StartsWith("http://")&&!M3USourceURLTb.Text.StartsWith("https://"))
        {
            await DisplayAlert("��ʾ��Ϣ", "��ʱֻ֧��HTTPЭ���URL��", "ȷ��");
            return;
        }


        M3UAnalysisBtn.IsEnabled=false;
        try
        {
            CheckValidModel videoCheckModel = (CheckValidModel)new XmlSerializer(typeof(CheckValidModel)).Deserialize(
await new HttpClient().GetStreamAsync("https://fclivetool.com/api/NewShareURL?url="+M3USourceURLTb.Text));

            if (videoCheckModel is null)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                M3UAnalysisBtn.IsEnabled=true;
                return;
            }

            switch(videoCheckModel.StatusCode)
            {
                case "0":
                    if (videoCheckModel.Result=="OK")
                    {
                        await DisplayAlert("��ʾ��Ϣ", "����ֱ��Դ�ɹ�����л���ķ���\n���ڽ��Զ����ص�һҳ��", "ȷ��");

                        VSLCurrentPageIndex=1;
                        await GetShareData(1);
                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "����ʧ�ܣ���Ϊֱ��Դ��ʧЧ��������Ϣ��"+videoCheckModel.Result, "ȷ��");
                    }

                    break;         
                case "-1":
                    await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ֱ��Դ��ַ��", "ȷ��");
                    break;     
                case "-2":
                    await DisplayAlert("��ʾ��Ϣ", "��ʱֻ֧��HTTPЭ���URL��", "ȷ��");
                    break;     
                default:
                    await DisplayAlert("��ʾ��Ϣ", "���ֱ��Դ��Ч��ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                    break;
            }


        }
        catch (Exception)
        {
            await DisplayAlert("��ʾ��Ϣ", "���ֱ��Դ��Ч��ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
        }

        M3UAnalysisBtn.IsEnabled=true;
    }

    private async void M3UItemRightBtn_Clicked(object sender, EventArgs e)
    {
        Button UpdBtn = sender as Button;

        if (!string.IsNullOrWhiteSpace(UpdBtn.CommandParameter.ToString()))
        {
            UpdBtn.IsEnabled=false;

            if (UpdBtn.StyleId=="M3UStateUpdateBtn")
            {
                try
                {
                    CheckValidModel videoCheckModel = (CheckValidModel)new XmlSerializer(typeof(CheckValidModel)).Deserialize(
        await new HttpClient().GetStreamAsync("https://fclivetool.com/api/UpdateShareURL?url="+UpdBtn.CommandParameter.ToString()));

                    if (videoCheckModel is null)
                    {
                        await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                        UpdBtn.IsEnabled=true;
                        return;
                    }

                    switch (videoCheckModel.StatusCode)
                    {
                        case "0":
                            if (videoCheckModel.Result=="OK")
                            {
                                await DisplayAlert("��ʾ��Ϣ", "ֱ��Դ��Ч��", "ȷ��");

                                //await GetShareData(1);
                            }
                            else
                            {
                                await DisplayAlert("��ʾ��Ϣ", "ֱ��Դ��ʧЧ�������ѱ��Ƴ���������Ϣ��"+videoCheckModel.Result, "ȷ��");

                                VSLCurrentPageIndex=1;
                                await GetShareData(1);
                            }

                            break;
                        default:
                            await DisplayAlert("��ʾ��Ϣ", "���ֱ��Դ��Ч��ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                            break;
                    }


                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "���ֱ��Դ��Ч��ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                }

            }
            else if (UpdBtn.StyleId=="M3UPlayBtn")
            {
                try
                {
                    string readresult = await new VideoManager().DownloadAndReadM3U8File(M3U8PlayList, new string[] { "", UpdBtn.CommandParameter.ToString() });
                    if (readresult!="")
                    {
                        await DisplayAlert("��ʾ��Ϣ", readresult, "ȷ��");
                        UpdBtn.IsEnabled=true;
                        return;
                    }


                    M3U8PlayList.Insert(0, new string[] { "Ĭ��", UpdBtn.CommandParameter.ToString() });
                    string[] MOptions = new string[M3U8PlayList.Count];
                    MOptions[0]="Ĭ��\n";
                    string WantPlayURL = UpdBtn.CommandParameter.ToString();

                    if (M3U8PlayList.Count > 2)
                    {
                        for (int i = 1; i<M3U8PlayList.Count; i++)
                        {
                            MOptions[i]="��"+i+"��\n�ļ�����"+M3U8PlayList[i][0]+"\nλ�ʣ�"+M3U8PlayList[i][2]+"\n�ֱ��ʣ�"+M3U8PlayList[i][3]+"\n֡�ʣ�"+M3U8PlayList[i][4]+"\n���������"+M3U8PlayList[i][5]+"\n��ǩ��"+M3U8PlayList[i][6]+"\n";
                        }

                        string MSelectResult = await DisplayActionSheet("��ѡ��һ��ֱ��Դ��", "ȡ��", null, MOptions);
                        if (MSelectResult == "ȡ��"||MSelectResult is null)
                        {
                            UpdBtn.IsEnabled=true;
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
                    VideoPrevPage.videoPrevPage.NowPlayingTb.Text="����ֱ��Դ";

                    //����������תҳ��ǰ��Ҫ�Ƚ��ã������û��ٴη��ص�����ҳ��ʱ���������Ż����
                    UpdBtn.IsEnabled=true;

                    var mainpage = ((Shell)App.Current.MainPage);
                    mainpage.CurrentItem = mainpage.Items.FirstOrDefault();
                    await mainpage.Navigation.PopToRootAsync();

                }
                catch (Exception)
                {
                    await DisplayAlert("��ʾ��Ϣ", "���ֱ��Դ��Ч��ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                }

            }
            else if (UpdBtn.StyleId=="M3UDownloadBtn")
            {
                string[] options = new string[2];
                using (Stream stream = await new VideoManager().DownloadM3U8FileToStream(UpdBtn.CommandParameter.ToString(), options))
                {
                    if (stream is null)
                    {
                        await DisplayAlert("��ʾ��Ϣ", options[0], "ȷ��");
                        UpdBtn.IsEnabled=true;
                        return;
                    }

#if ANDROID
                        await DisplayAlert("��ʾ��Ϣ", "��׿��δ����", "ȷ��");
                        UpdBtn.IsEnabled=true;
                        return;
#else
                    var fileSaver = await FileSaver.SaveAsync(FileSystem.AppDataDirectory, options[1], stream, CancellationToken.None);

                    if (fileSaver.IsSuccessful)
                    {
                        await DisplayAlert("��ʾ��Ϣ", "�ļ��ѳɹ���������\n" + fileSaver.FilePath, "ȷ��");
                    }
                    else
                    {
                        await DisplayAlert("��ʾ��Ϣ", "����ȡ���˲�����", "ȷ��");
                    }

#endif

                }

            }


            UpdBtn.IsEnabled=true;
        }

    }

    private async void VideoShareList_Refreshing(object sender, EventArgs e)
    {
        VSLCurrentPageIndex=1;
        await GetShareData(1);
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        if(videoSharePage!=null)
        {
            return;
        }
        videoSharePage=this;

        await GetShareData(1);
    }

    public async Task GetShareData(int pageindex)
    {
        VideoShareListRing.IsRunning = true;
        VSLPagePanel.IsVisible=false;

        if (pageindex <= 0)
        {
            await DisplayAlert("��ʾ��Ϣ", "ҳ�����벻��ȷ��", "ȷ��");
            VideoShareListRing.IsRunning = false;
            return;
        }

        try
        {
            var getresult = await new HttpClient().GetStringAsync("https://fclivetool.com/api/GetShareURL?pageindex="+pageindex);
            CheckValidModel videoCheckModel = (CheckValidModel)new XmlSerializer(typeof(CheckValidModel)).Deserialize(new StringReader(getresult));
            //CheckValidModel videoCheckModel = (CheckValidModel)new XmlSerializer(typeof(CheckValidModel)).Deserialize(await new HttpClient().GetStreamAsync("https://fclivetool.com/api/GetShareURL?pageindex="+pageindex));

            if (videoCheckModel is null)
            {
                await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
                VideoShareListRing.IsRunning = false;
                return;
            }

            if(videoCheckModel.StatusCode=="0")
            {
                if(videoCheckModel.Result=="OK")
                {
                    VideoShareList.ItemsSource= videoCheckModel.Content;

                    VSLPagePanel.IsVisible=true;
                }
            }
            else if(videoCheckModel.StatusCode=="-3")
            {
                await DisplayAlert("��ʾ��Ϣ", "û�е�ǰҳ�롣", "ȷ��");

                VSLCurrentPageIndex=1;
                await GetShareData(1);
            }
            else
            {
                await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("��ʾ��Ϣ", "��ȡ����ʱ�����쳣�����Ժ����ԣ�", "ȷ��");
        }

        VideoShareListRing.IsRunning = false;

    }

    private async void M3URefreshBtn_Clicked(object sender, EventArgs e)
    {
        VSLCurrentPageIndex=1;
        await GetShareData(1);
    }

    private async void VSLBackBtn_Clicked(object sender, EventArgs e)
    {
        if (VSLCurrentPageIndex<=1)
        {
            await DisplayAlert("��ʾ��Ϣ", "û����һҳ�ˣ�", "ȷ��");
            return;
        }

        VSLCurrentPageIndex--;
        await GetShareData(VSLCurrentPageIndex);
    }

    private async void VSLJumpBtn_Clicked(object sender, EventArgs e)
    {
        int TargetPage = 1;
        if (!int.TryParse(VSLPageTb.Text, out TargetPage))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }
        //��ʱ��������ж�
        if (TargetPage<1)
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ȷ��ҳ�룡", "ȷ��");
            return;
        }

        VSLCurrentPageIndex=TargetPage;
        await GetShareData(VSLCurrentPageIndex);
    }

    private async void VSLNextBtn_Clicked(object sender, EventArgs e)
    {
        //��ʱ��������ж�

        VSLCurrentPageIndex++;
        await GetShareData(VSLCurrentPageIndex);
    }
}