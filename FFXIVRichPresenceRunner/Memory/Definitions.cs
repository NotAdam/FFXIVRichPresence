﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FFXIVRichPresenceRunner.Memory
{
    class Definitions
    {
        private static Definitions _cachedInstance = null;
        private const string DEFINITION_JSON_URL = "https://raw.githubusercontent.com/goaaats/FFXIVPlayerWardrobe/master/definitiones.json";

        public static Definitions Instance
        {
            get
            {
                if (_cachedInstance != null)
                    return _cachedInstance;

                using (WebClient client = new WebClient())
                {
                    try
                    {
                        var result = client.DownloadString(DEFINITION_JSON_URL);
                        _cachedInstance = JsonConvert.DeserializeObject<Definitions>(result);

                        return _cachedInstance;
                    }
                    catch (Exception)
                    {
                        _cachedInstance = new Definitions();
                        return _cachedInstance;
                    }
                }
            }
        }

        public static string Json => JsonConvert.SerializeObject(new Definitions());

        public string TIMEOFFSETPTR = "ffxiv_dx11.exe+18E3330,10,8,28,80"; // 4 byte
        public string WEATHEROFFSETPTR = "ffxiv_dx11.exe+18E1278,27"; // 1 byte
        public string TERRITORYTYPEOFFSETPTR = "ffxiv_dx11.exe+1936928,4C"; // 4 byte

        public string ACTORTABLEOFFSET = "ffxiv_dx11.exe+18FF6B8";

        public string ClientID = "478143453536976896";
    }
}