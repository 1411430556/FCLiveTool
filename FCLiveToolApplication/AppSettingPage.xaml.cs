namespace FCLiveToolApplication;

public partial class AppSettingPage : ContentPage
{
	public AppSettingPage()
	{
		InitializeComponent();
	}
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Navigation.PopAsync();
    }

    private async void ShowDPInput_Clicked(object sender, EventArgs e)
    {
        string urlnewvalue = await DisplayPromptAsync("����Ĭ��ֵ", "��1�� �������µ�ֱ��ԴURL��", "����", "ȡ��", "URL...", -1, Keyboard.Text, "");
        if (string.IsNullOrWhiteSpace(urlnewvalue))
        {
            if(urlnewvalue!=null)
                await DisplayAlert("��ʾ��Ϣ", "��������ȷ�����ݣ�", "ȷ��");
            return;
        }
        if(!urlnewvalue.Contains("://"))
        {
            await DisplayAlert("��ʾ��Ϣ", "��������ݲ�����URL�淶��", "ȷ��");
            return;
        }

        string namenewvalue = await DisplayPromptAsync("����Ĭ��ֵ", "��2�� �������µ����ƣ�", "����", "ȡ��", "����...", -1, Keyboard.Text, "");
        if (string.IsNullOrWhiteSpace(namenewvalue))
        {
            if (namenewvalue!=null)
                await DisplayAlert("��ʾ��Ϣ", "��������ȷ�����ݣ�", "ȷ��");
            return;
        }

        Preferences.Set("DefaultPlayM3U8URL", urlnewvalue);
        DefaultPlayM3U8URLText.Text=urlnewvalue;
        Preferences.Set("DefaultPlayM3U8Name", namenewvalue);
        DefaultPlayM3U8NameText.Text=namenewvalue;

        await DisplayAlert("��ʾ��Ϣ", "���óɹ����ٴδ�APP�󼴿���Ч��", "ȷ��");
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        DefaultPlayM3U8NameText.Text=Preferences.Get("DefaultPlayM3U8Name", "");
        DefaultPlayM3U8URLText.Text=Preferences.Get("DefaultPlayM3U8URL", "");
        StartAutoPlayToogleBtn.IsToggled= Preferences.Get("StartAutoPlayM3U8", true);
        DarkModeToogleBtn.IsToggled= Preferences.Get("AppDarkMode", false);
    }

    private void StartAutoPlayToogleBtn_Toggled(object sender, ToggledEventArgs e)
    {
        if(e.Value)
        {
            Preferences.Set("StartAutoPlayM3U8", true);
        }
        else
        {
            Preferences.Set("StartAutoPlayM3U8", false);
        }
    }

    private void DarkModeToogleBtn_Toggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            Preferences.Set("AppDarkMode", true);
            Application.Current.UserAppTheme = AppTheme.Dark;
        }
        else
        {
            Preferences.Set("AppDarkMode", false);
            Application.Current.UserAppTheme = AppTheme.Light;
        }
    }
}