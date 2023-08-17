namespace FCLiveToolApplication;

public partial class AboutPage : ContentPage
{
	public AboutPage()
	{
		InitializeComponent();
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Navigation.PopAsync();
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        AppVersionText.Text=VersionTracking.CurrentVersion;
    }

    private void GitHubBtn_Clicked(object sender, EventArgs e)
    {
        Launcher.OpenAsync("https://github.com/FHWWC/FCLiveTool");
    }

    private async void EmailBtn_Clicked(object sender, EventArgs e)
    {
        if(!await Launcher.TryOpenAsync("mailto:justineedyoumost@163.com"))
        {
            await DisplayAlert("��ʾ��Ϣ", "�޷����ʼ��ͻ��ˣ�������Ҫ���ֶ��������䣺\n justineedyoumost@163.com", "ȷ��");
        }
    }

    private void GroupsBtn_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("��ʾ��Ϣ", "���ǵ�Ⱥ�飺\n", "ȷ��");
    }
}