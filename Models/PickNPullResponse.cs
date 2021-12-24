using System;
using System.Collections.Generic;

namespace Junkyard;

[Serializable]
class PickNPullResponse
{
    public Location location { get; set; }
    public List<CarInfo> vehicles { get; set; }
}