using LamestWebserver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.UI;
using LamestWebserver.Caching;
using LamestWebserver.UI.CachedByDefault;
using Demos.HelperClasses;

namespace Demos
{
    public class Tut03 : CachedResponse
    {
        public Tut03() : base(nameof(Tut03))
        {
        }

        protected override HElement GetContents(SessionData sessionData)
        {
            HSelectivelyCacheableElement page = MainPage.GetPage(GetPageContents(), nameof(Tut03) + ".cs");
            page.CachingType = ECachingType.Cacheable;

            return page;
        }

        private IEnumerable<HElement> GetPageContents()
        {
            yield return new HHeadline("Caching HElements & Responses") { CachingType = ECachingType.Cacheable };
            yield return new HText("LamestWebserver provides some functionality for Caching UI components and responses. This page will provide an overview of the functionality and explain the basics.") { CachingType = ECachingType.Cacheable };

            yield return new HHeadline("Caching UI Elements", 2) { CachingType = ECachingType.Cacheable };
            yield return new HText($"To actually use cached UI elements they need to be inside a {nameof(HCachePool)} or a ") { Class = "warning" }.SetCacheable();
            yield return new HText("Fist of all, UI elements inherit from one of two classes which determine the caching behaviour of these elements:").SetCacheable();

            yield return new HList(HList.EListType.UnorderedList, 
                new CContainer(
                    new CHeadline(nameof(HCacheableElement), 3), 
                    new CText($"UI elements inheriting from {nameof(HCacheableElement)} are fully cacheable like {nameof(HNewLine)}.")),
                new CContainer(
                    new CHeadline(nameof(HSelectivelyCacheableElement), 3),
                    new CText($"UI elements inheriting from {nameof(HSelectivelyCacheableElement)} contain a field '{nameof(HSelectivelyCacheableElement.CachingType)}' which displays the cachablility of a certain element. You have to set this field manually if you want the element to be cached."),
                    new CText($"Nearly every element in {nameof(LamestWebserver)}.{nameof(LamestWebserver.UI)} inheriting from {nameof(HSelectivelyCacheableElement)} has an automatically caching counterpart in {nameof(LamestWebserver)}.{nameof(LamestWebserver.UI)}.{nameof(LamestWebserver.UI.CachedByDefault)}, like {nameof(HText)} hast a counterpart called {nameof(CText)} which has chaching enabled by default so you don't have to set it every single time."),
                    new HCodeElement(
                        $"using {nameof(LamestWebserver)}.{nameof(LamestWebserver.UI)};",
                        "", "// The default way of setting things cacheable:",
                        $"new {nameof(HText)}(\"Cache Me!\") {{ {nameof(HSelectivelyCacheableElement.CachingType)} = {nameof(ECachingType)}.{nameof(ECachingType.Cacheable)} }};",
                        "", "",
                        $"using {nameof(LamestWebserver)}.{nameof(LamestWebserver.UI)}.{nameof(LamestWebserver.UI.CachedByDefault)};",
                        "", 
                        $"// The faster way of setting an element cacheable if there's an equivalent in {nameof(LamestWebserver)}.{nameof(LamestWebserver.UI)}.{nameof(LamestWebserver.UI.CachedByDefault)}:",
                        $"new {nameof(CText)}(\"Cache Me!\");"
                        ).SetCacheable())
                ).SetCacheable();

            yield return new CText($"UI elements not inheriting from one of them are usually not cacheable at all.");
            yield return new CText($"If you want to have some elements inside a Container or {nameof(HMultipleElements)} not-cached and other's cached, LamestWebserver will automatically figure that out if they are inside a cachable response or a {nameof(HCachePool)} and their Container or {nameof(HMultipleElements)} is set to {nameof(ECachingType)}.{nameof(ECachingType.Cacheable)} or your Container is a {nameof(CContainer)} which is set to be {nameof(ECachingType)}.{nameof(ECachingType.Cacheable)} by default.");

            yield return new CContainer(
                new CHeadline("Example", 3),
                new CText($"This is a {nameof(CText)} UI element. The contents of this element are cached:"),
                new CText("The current time is: " + DateTime.Now.ToLongTimeString()) { Class = "smallcode" },
                new CText($"This is a {nameof(HText)} UI element. The contents of this element are not cached and should change if you reload the page:"),
                new HText("The current time is: " + DateTime.Now.ToLongTimeString()) { Class = "smallcode" });
            yield return new HNewLine();


            yield return new CHeadline($"Caching using {nameof(HCachePool)}", 2);
            yield return new CText($"The easy way to get cached responses in your project is to use a {nameof(HCachePool)}. It wrapps any HElement to use the caching functions (if applicable) using {nameof(HElement)}.ToString() or {nameof(HElement)}.{nameof(HElement.GetContent)}({nameof(SessionData)} sessionData).");
            yield return new CText($"If you have multiple {nameof(HCachePool)}s on the same page remember to pass them different indexes or they will use the same cache position and override their cached elements.");

            yield return new HCachePool(new CContainer(
                new CHeadline("Example", 3),
                new CText($"This is a {nameof(CContainer)} inside a {nameof(HCachePool)} with cached and non-cached subelements."),
                new CText("The cached current time is: " + DateTime.Now.ToLongTimeString()) { Class = "smallcode" },
                new HText("The non-cached current time is: " + DateTime.Now.ToLongTimeString()) { Class = "smallcode" }
                ), this, 0);
            yield return new HNewLine();


            yield return new CHeadline("Caching Responses", 2);
            yield return new CText($"To easily use cached elements everywhere in your project you can just use a {nameof(CachedResponse)} instead of an {nameof(ElementResponse)} or {nameof(PageResponse)} and all contents of your page will automatically be cached according to their respective {nameof(HSelectivelyCacheableElement.CachingType)} settings or the classes that they inherit from.");
            yield return new CText($"For example: This page is a {nameof(CachedResponse)} and all elements on this page are cached if specified.");
            yield return new HNewLine();
        }
    }
}
