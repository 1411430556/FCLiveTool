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
    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        if (videoSubPage != null)
        {
            return;
        }

        videoSubPage=this;
        ReadLocalSubList();
    }
    public async void ReadLocalSubList()
    {
        int permResult = await new APPPermissions().CheckAndReqPermissions();
        if (permResult!=0)
        {
            await DisplayAlert("��ʾ��Ϣ", "����Ȩ��ȡ��д��Ȩ�ޣ�������Ҫ����Ͷ�ȡ�ļ����������Ȩ�������漰�ļ���д�Ĳ������޷�����ʹ�ã�", "ȷ��");
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
                await DisplayAlert("��ʾ��Ϣ", "��ȡ��������ʱ����", "ȷ��");
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

        if(!await DisplayAlert("��ʾ��Ϣ","��Ҫɾ������ "+ button.CommandParameter.ToString()+" ��", "ȷ��", "ȡ��"))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(button.CommandParameter.ToString()))
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

                    var items = tlist.FirstOrDefault(p => p.SubName==button.CommandParameter.ToString());
                    if (tlist != null)
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
                    if (tlist != null)
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
    private void VSLUpdateBtn_Clicked(object sender, EventArgs e)
    {

    }
}