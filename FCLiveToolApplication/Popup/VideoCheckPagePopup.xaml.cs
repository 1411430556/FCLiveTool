using CommunityToolkit.Maui.Views;

namespace FCLiveToolApplication;

public partial class VideoCheckPagePopup : CommunityToolkit.Maui.Views.Popup
{
	public VideoCheckPagePopup()
	{
		InitializeComponent();
	}

    private void SaveOptionBtn_Clicked(object sender, EventArgs e)
    {
        #region �жϡ�ͬʱ�����߳�����
        if (string.IsNullOrWhiteSpace(UseThreadNumTb.Text))
        {
            VideoCheckPage.videoCheckPage.PopShowMsg("�㻹û�����롰ͬʱ�����߳�������");
            return;
        }
        int threadNum;
        if (!int.TryParse(UseThreadNumTb.Text, out threadNum))
        {
            VideoCheckPage.videoCheckPage.PopShowMsg("��ͬʱ�����߳�������������Ч����ֵ��");
            return;
        }
        if (threadNum<1)
        {
            threadNum=GlobalParameter.VideoCheckThreadNum;
        }
        #endregion

        #region �жϡ�User-Agent��
        if (string.IsNullOrWhiteSpace(UseUATb.Text))
        {
            VideoCheckPage.videoCheckPage.PopShowMsg("�㻹û�����롰User-Agent����");
            return;
        }
        #endregion

        Preferences.Set("VideoCheckThreadNum", threadNum);
        Preferences.Set("VideoCheckUA", UseUATb.Text);


        VideoCheckPage.videoCheckPage.PopShowMsg("�Ѹ��¼��ѡ��Ĳ�����");
        CloseAsync();
    }

    private async void ResetOptionBtn_Clicked(object sender, EventArgs e)
    {
        if (await VideoCheckPage.videoCheckPage.PopShowMsgAndReturn("��Ҫ��������ѡ��ΪĬ��ֵ��"))
        {
            Preferences.Set("VideoCheckThreadNum", GlobalParameter.VideoCheckThreadNum);
            Preferences.Set("VideoCheckUA", GlobalParameter.VideoCheckUA);

            await CloseAsync();
        }
    }

    private void MainGrid_Loaded(object sender, EventArgs e)
    {
        UseThreadNumTb.Text= Preferences.Get("VideoCheckThreadNum", GlobalParameter.VideoCheckThreadNum).ToString();
        UseUATb.Text= Preferences.Get("VideoCheckUA", GlobalParameter.VideoCheckUA);
    }
}