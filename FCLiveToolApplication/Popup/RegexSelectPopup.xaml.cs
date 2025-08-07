namespace FCLiveToolApplication.Popup;

public partial class RegexSelectPopup : CommunityToolkit.Maui.Views.Popup
{
    /// <summary>
    /// ҳ�湹�캯���������Զ�ѡ�񷽰�
    /// </summary>
    /// <param name="popupType">0����VideoListPage; 1����VideoCheckPage; 2����VideoSubPage;</param>
    /// <param name="setRegexIndex">ָ��һ������</param>
    /// <param name="recommendRegex">����ֱ��Դ��ԭʼ���ݣ���õ��Ƽ�ѡ��ķ���</param>
    public RegexSelectPopup(int popupType, int setRegexIndex, string recommendRegex)
    {
        InitializeComponent();

        PopupType = popupType;
        SetRegexIndex= setRegexIndex;
        RecommendRegex = recommendRegex;

        InitRegexList();
    }

    /// <summary>
    /// Ҫ�������б����ڵ�ҳ��
    /// </summary>
    public int PopupType;
    /// <summary>
    /// ָ��һ������
    /// </summary>
    public int SetRegexIndex;
    /// <summary>
    /// �Ƽ�ѡ��ķ���
    /// </summary>
    public string RecommendRegex;
    public bool isOKBtnClicked;

    public void InitRegexList()
    {
        List<string> RegexOption = new List<string>() { "����1", "����2", "����3", "����4", "����5" };
        RegexSelectBox.ItemsSource = RegexOption;

        RegexSelectBox.SelectedIndex=SetRegexIndex;
        RecommendRegexTb.Text=RecommendRegex;
    }
    private void SaveOptionBtn_Clicked(object sender, EventArgs e)
    {
        if (PopupType == 1)
        {
            VideoCheckPage.videoCheckPage.RegexSelectIndex=RegexSelectBox.SelectedIndex;
            VideoCheckPage.videoCheckPage.RegexOption1=RegexOptionCB.IsChecked;
        }
        else if (PopupType == 2)
        {
            VideoSubPage.videoSubPage.RegexSelectIndex=RegexSelectBox.SelectedIndex;
            VideoSubPage.videoSubPage.RegexOption1=RegexOptionCB.IsChecked;
        }

        isOKBtnClicked = true;
        this.CloseAsync();
    }

    private void CancelBtn_Clicked(object sender, EventArgs e)
    {
        //��ʱ����ѯ����ʾ��
        isOKBtnClicked = false;
        this.CloseAsync();
    }

    private void RegexSelectTipBtn_Clicked(object sender, EventArgs e)
    {
        VideoPrevPage.videoPrevPage.PopShowMsg("������Ϣ", new MsgManager().GetRegexOptionTip(), "�ر�");
    }
}