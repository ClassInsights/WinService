using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WinService.Models;

namespace WinService;

public class AutoShutdown
{
    public readonly Api Api;
    public AutoShutdown()
    {
        Api = new Api();
    }

    public async Task RunAsync()
    {
        var room = await Api.GetRoomAsync("DV2");
        var computer = await Api.UpdateComputer(new RequestModels.ComputerRequest()
        {
            LastSeen = DateTime.Now,
            Name = room.Name,
            Room = room.Id
        });
    }

    public async Task StopAsync()
    {

    }
}