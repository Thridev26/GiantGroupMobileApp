using Microsoft.Maui.Controls;

namespace GiantGroupMobileApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnQuoteClicked(object sender, EventArgs e)
        {
            await DisplayAlert("The Giant Group", "Redirecting to quote request or contact line (0848 331 373)...", "OK");
        }

        private async void OnOpenOakChatClicked(object sender, EventArgs e)
        {
            // Simple mobile equivalent popup mimicking Oak's FAQ assistant logic from Site.Master
            string question = await DisplayPromptAsync("Oak Assistant", "Hi, I'm Oak! Ask me about pricing, working hours, or our services:", placeholder: "e.g. What are your working hours?");

            if (!string.IsNullOrWhiteSpace(question))
            {
                string lower = question.ToLower();
                string reply = "Sorry, I'm not sure about that. Call our team directly at 0848 331 373.";

                if (lower.Contains("cost") || lower.Contains("rate") || lower.Contains("price") || lower.Contains("quote"))
                {
                    reply = "Our pricing depends on the size and complexity of the tree-felling job. Contact us for a free on-site quote!";
                }
                else if (lower.Contains("hours") || lower.Contains("open") || lower.Contains("time") || lower.Contains("working"))
                {
                    reply = "We operate Monday to Saturday, from 7:00 AM to 5:00 PM.";
                }
                else if (lower.Contains("service") || lower.Contains("tree") || lower.Contains("felling"))
                {
                    reply = "We specialize in professional tree-felling, stump removal, site clearance, and general garden maintenance.";
                }

                await DisplayAlert("Oak says:", reply, "Close");
            }
        }
    }
}