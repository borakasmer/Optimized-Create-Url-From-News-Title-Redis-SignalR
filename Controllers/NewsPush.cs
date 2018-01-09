using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class NewsPush : Hub
    {        
        public Task PushNews(News news)
        {
            return Clients.All.InvokeAsync("GetNews", news);
        }         
    }