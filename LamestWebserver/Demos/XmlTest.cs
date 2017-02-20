using LamestWebserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.Serialization;
using LamestWebserver.UI;

namespace Demos
{
    public static class XmlTest
    {
        private static List<dataPiece> dataValues = new List<dataPiece>();

        public static void register()
        {
            new Displayer();
            new Adder();
        }

        public class dataPiece : ISerializable
        {
            public static Random rand = new Random();

            public string name, data;
            public int age;
            public List<string> ancestors;

            public dataPiece(string name, string data, int age)
            {
                this.name = name;
                this.data = data;
                this.age = age;

                this.ancestors = new List<string>();

                int ancestorCount = (int)(rand.NextDouble() * 3 + 2);

                if (dataValues.Count > ancestorCount * 1.5f)
                {
                    for (int i = 0; i < ancestorCount; i++)
                    {
                        int nextAncestor = (int)(rand.NextDouble() * (dataValues.Count - 1));
                        ancestors.Add(dataValues[nextAncestor].name);
                    }
                }
            }

            public dataPiece(SerializationInfo info)
            {
                name = info.GetString(nameof(name));
                data = info.GetString(nameof(data));
                age = info.GetInt32(nameof(age));
                ancestors = (List<string>)info.GetValue(nameof(ancestors), ancestors.GetType());
            }

            public dataPiece()
            {
                ancestors = new List<string>();
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(name), name);
                info.AddValue(nameof(data), data);
                info.AddValue(nameof(age), age);
                info.AddValue(nameof(ancestors), ancestors);
            }
        }

        public class Displayer : PageBuilder
        {
            public Displayer() : base("XML Test", "xmltest")
            {
                Elements = new List<HElement>()
                {
                    HRuntimeCode.getConditionalRuntimeCode(new HText("DESERIALIZATION FAILED!\n"), new HText(), (sessionData) => { return (sessionData is SessionData && (sessionData as SessionData).GetHttpHeadValue("fail") != null); }),
                    HRuntimeCode.getConditionalRuntimeCode(new HText("SERIALIZATION FAILED!\n"), new HText(), (sessionData) => { return (sessionData is SessionData && (sessionData as SessionData).GetHttpHeadValue("sfail") != null); }),
                    new HLink("add element", "xmladd"),
                    new HNewLine(),
                    new HLink("serialize",
                        InstantPageResponse.AddOneTimeConditionalRedirect("xmltest", "xmltest?sfail", false, (SessionData sessionData) =>
                            { try { Serializer.WriteXmlData(dataValues, "xmltest.xml"); return true; } catch(Exception) { return false; } })),
                    new HNewLine(),
                    new HLink("deserialize",
                        InstantPageResponse.AddOneTimeConditionalRedirect("xmltest", "xmltest?fail", false, (SessionData sessionData) =>
                            { try { dataValues = Serializer.ReadXmlData<List<dataPiece>>("xmltest.xml"); return true; } catch(Exception) { return false; } })),
                    new HNewLine(),
                    new HRuntimeCode((sessionData) =>
                        {
                            string s = "";

                            for(int i = 0; i < dataValues.Count; i++)
                            {
                                s += new HList(HList.EListType.UnorderedList, dataValues[i].name.ToHElement(), dataValues[i].age.ToHElement(), dataValues[i].data.ToHElement(), new HList(HList.EListType.OrderedList, dataValues[i].ancestors)) * sessionData;
                            }

                            return s;
                        })
                };
            }
        }

        public class Adder : PageResponse
        {
            public Adder() : base("xmladd") { }

            protected override string GetContents(SessionData sessionData)
            {
                return new HForm(InstantPageResponse.AddOneTimeRedirectWithCode("xmltest", true, (SessionData sessionData_) =>
                {
                    int age = -1;

                    if (sessionData_.GetHttpPostValue("name") != null && int.TryParse(sessionData_.GetHttpPostValue("age"), out age) && sessionData_.GetHttpPostValue("data") != null)
                    {
                        dataValues.Add(new dataPiece(sessionData_.GetHttpPostValue("name"), sessionData_.GetHttpPostValue("data"), age));
                    }
                }))
                {
                    Elements = new List<HElement>()
                    {
                        new HHeadline("Add an item"),
                        new HLine(),
                        new HPlainText("name:"),
                        new HNewLine(),
                        new HInput(HInput.EInputType.text, "name"),
                        new HNewLine(),
                        new HPlainText("age:"),
                        new HNewLine(),
                        new HInput(HInput.EInputType.number, "age"),
                        new HNewLine(),
                        new HPlainText("data:"),
                        new HNewLine(),
                        new HInput(HInput.EInputType.text, "data"),
                        new HLine(),
                        new HButton("Add", HButton.EButtonType.submit)
                    }
                }
                .GetContent(sessionData);
            }
        }
    }
}
