using System;
using System.Collections.Generic;

namespace LameNetHook
{
    public static class SessionContainer
    {
        private static List<string> FileHashes = new List<string>();
        private static List<List<Object>> FileObjects;
        private static List<List<string>> FileObjectNames;

        private static List<string> UserHashes;
        private static List<List<Object>> UserObjects;
        private static List<List<string>> UserObjectNames;

        private static List<List<string>> FilePerUserHashes;
        private static List<List<List<Object>>> FilePerUserObjects;
        private static List<List<List<string>>> FilePerUserObjectHashes;
    }

    public struct SessionData
    {
        public int UserToken, FileToken, UserFileToken;
        private List<string> additionalHEAD;
        private List<string> additionalPOST;
        private List<string> valuesHEAD;
        private List<string> valuesPOST;

        public SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST) : this()
        {
            this.additionalHEAD = additionalHEAD;
            this.additionalPOST = additionalPOST;
            this.valuesHEAD = valuesHEAD;
            this.valuesPOST = valuesPOST;
        }

        public string getValueHead(string name)
        {
            for (int i = 0; i < additionalHEAD.Count; i++)
            {
                if (name == additionalHEAD[i])
                    return valuesHEAD[i];
            }

            return null;
        }

        public string getValuePost(string name)
        {
            for (int i = 0; i < additionalPOST.Count; i++)
            {
                if (name == additionalPOST[i])
                    return valuesPOST[i];
            }

            return null;
        }
    }
}