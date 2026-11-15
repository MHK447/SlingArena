#if UNITY_IOS || UNITY_IPHONE
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using UnityEngine;

namespace BanpoFri
{
    public class PostProcess
    {
        private const string LegacyResourcesDirectoryName = "Resources";
        private const string AppLovinMaxResourcesDirectoryName = "AppLovinMAXResources";

        [PostProcessBuildAttribute(int.MaxValue)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            plist.root.SetString("CFBundleDisplayName", PlayerSettings.productName);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("NSLocationWhenInUseUsageDescription", "A simple text that describes why your app needs the location");
            plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com/");

            // ATT 시스템 팝업 기본(폴백) 문구 — 기기 언어별 문구는 InfoPlist.strings 로컬라이즈로 덮어씀
            plist.root.SetString("NSUserTrackingUsageDescription",
                "This uses device info for more personalized ads and content");

            // 광고 네트워크별 SKAdNetwork ID 목록.
            // 주의: Apple은 ID를 모두 소문자로 요구하며, 누락되면 해당 네트워크 어트리뷰션이 보고되지 않음.
            // 추가/수정 시 알파벳순으로 정렬해 중복 검수 용이하게 유지할 것.
            var ids = new string[] {
                "22mmun2rn5",
                "238da6jt44",
                "23zd986j2c",
                "24t9a8vw3c",
                "24zw6aqk47",
                "252b5q8x7y",
                "275upjj5gd",
                "294l99pt4k",
                "2fnua5tdw4",
                "2q884k2j68",
                "2rq3zucswp",
                "2tdux39lx8",
                "2u9pt9hc89",
                "32z4fx6l9h",
                "33r6p7g8nc",
                "3cgn6rq224",
                "3l6bd9hu43",
                "3qcr597p9d",
                "3qy4746246",
                "3rd42ekr43",
                "3sh42y64q3",
                "424m5254lk",
                "4468km3ulz",
                "44jx6755aq",
                "44n7hlldy6",
                "47vhws6wlr",
                "488r3q3dtq",
                "4dzt52r2t5",
                "4fzdc2evr5",
                "4mn522wn87",
                "4pfyvq9l8r",
                "4w7y6s5ca2",
                "523jb4fst2",
                "52fl2v3hgk",
                "54nzkqm89y",
                "55644vm79v",
                "55y65gfgn7",
                "577p5t736z",
                "578prtvx9j",
                "5a6flpkh64",
                "5f5u5tfb26",
                "5ghnmfs3dh",
                "5l3tpt7t6e",
                "5lm9lj6jb7",
                "5mv394q32t",
                "5tjdwbrq8w",
                "627r9wr2y5",
                "633vhxswh4",
                "67369282zy",
                "6964rsfnh4",
                "6g9af3uyq4",
                "6p4ks3rnbw",
                "6qx585k4p6",
                "6rd35atwn8",
                "6v7lgmsu45",
                "6xzpu9s2p8",
                "6yxyv74ff7",
                "737z793b9f",
                "74b6s63p6l",
                "77y3x8wds4",
                "7953jerfzd",
                "79pbpufp6p",
                "79w64w269u",
                "7bxrt786m8",
                "7fbxrn65az",
                "7fmhfwg9en",
                "7k3cvf297u",
                "7rz58n8ntl",
                "7tnzynbdc7",
                "7ug5zh24hu",
                "84993kbrcf",
                "866k9ut3g3",
                "87u5trcl3r",
                "88k8774x49",
                "899vrgt9g8",
                "89z7zv988g",
                "8c4e2ghe7u",
                "8m87ys6875",
                "8qiegk9qfv",
                "8r8llnkz5a",
                "8s468mfl3y",
                "8w3np9l82g",
                "97r2b46745",
                "9b89h5y424",
                "9g2aggbj52",
                "9nlqeag3gk",
                "9rd848q2bz",
                "9t245vhmpl",
                "9vvzujtq5s",
                "9wsyqb3ku7",
                "9yg77x724h",
                "a2p9lx4jpn",
                "a7xqa6mtl2",
                "a8cz6cu7e5",
                "apzhy3va96",
                "au67k4efj4",
                "av6w8kgt66",
                "axh5283zss",
                "b53axzn49s",
                "b55w3d8y8z",
                "b9bk5wbcq9",
                "bvpn9ufa9b",
                "bxvub5ada5",
                "c3frkrj4fj",
                "c6k4g5qg8m",
                "c7g47wypnu",
                "cad8qz2s3j",
                "ce8ybjwass",
                "cg4yq2srnc",
                "cj5566h2ga",
                "cp8zw746q7",
                "cs644xg564",
                "cstr6suwn9",
                "cwn433xbcr",
                "d7g9azk84q",
                "dbu4b84rxf",
                "dd3a75yxkv",
                "dkc879ngq3",
                "dmv22haz9p",
                "dn942472g5",
                "dr774724x4",
                "dt3cjx1a9i",
                "dticjx1a9i",
                "dzg6xy7pwj",
                "e5fvkxwrpn",
                "ecpz2srf59",
                "eh6m2bh4zr",
                "ejvt5qm6ak",
                "eqhxz8m8av",
                "f2zub97jtl",
                "f38h382jlk",
                "f3h7jpbg97",
                "f73kdq92p3",
                "f7s53z58qe",
                "feyaarzu9v",
                "fkak3gfpt6",
                "fq6vru337s",
                "fz2k2k5tej",
                "g28c52eehv",
                "g2y4y55b64",
                "g3y426g9ed",
                "g69uk9uh2b",
                "g6gcrrvk4p",
                "gfat3222tu",
                "ggvn48r87g",
                "glqzh8vgby",
                "gta8lk7p23",
                "gta9lk7p23",
                "gvmwg8q7h5",
                "h5jmj969g5",
                "h65wbv5k3f",
                "h8vml93bkz",
                "hb56zgv37p",
                "hdw39hrw9y",
                "hjevpa356n",
                "hs6bdukanm",
                "jb7bn6koa5",
                "jk2fsx2rgz",
                "k674qkevps",
                "k6y4y55b64",
                "kbd757ywx3",
                "kbmxgpxpgc",
                "klf5c3l5u5",
                "krvm3zuq6h",
                "l6nv3x923s",
                "l93v5h6a4m",
                "ln5gz23vtd",
                "lr83yxwka7",
                "ludvb6z3bs",
                "m297p6643m",
                "m5mvw97r93",
                "m8dbw4sv7c",
                "mj797d8u6f",
                "mlmmfzh3r3",
                "mls7yz5dvl",
                "mp6xlyr22a",
                "mqn7fxpca7",
                "mtkv5xtk9e",
                "mwcgp9hppj",
                "n38lu8286q",
                "n66cz3y3bx",
                "n6fk4nfna4",
                "n9x2a789qt",
                "nfqy3847ph",
                "nrt9jy4kw9",
                "ns5j362hk7",
                "nu4557a4je",
                "nzq8sh4pbs",
                "p78axxw29g",
                "pd25vrrwzn",
                "ppxm28t8ap",
                "prcb7njmu6",
                "pt89h2hlb7",
                "pu4na253f3",
                "pwa73g5rt2",
                "pwdxu55a5a",
                "qlbq5gtkt8",
                "qqp299437r",
                "qu637u8glc",
                "qwpu75vrh2",
                "r26jy69rpl",
                "r45fhb6rf7",
                "r8lj5b58b5",
                "rvh3l7un93",
                "rx5hdcabgc",
                "s39g8k73mm",
                "s69wq72ugq",
                "sczv5946wb",
                "su67r6k2v3",
                "t38b2kh725",
                "t3b3f7n3x8",
                "t6d3zquu66",
                "t7ky8fmwkd",
                "tl55sbb4fm",
                "tmhh9296z4",
                "tskbem2b5g",
                "tvvz7th9br",
                "u679fj5vs4",
                "uw77j35x4d",
                "uzqba5354d",
                "v4nxqhlyqp",
                "v72qych5uu",
                "v7896pgt74",
                "v79kvwwj4g",
                "v9wttpbfk9",
                "vc83br9sjg",
                "vcra2ehyfk",
                "vhf287vqwu",
                "vutu7akeur",
                "w28pnjg2k4",
                "w7jznl3r6g",
                "w9q455wk68",
                "wg4vff78zm",
                "wzmmz9fp6w",
                "x2jnk7ly8j",
                "x44k69ngh6",
                "x5854y7y24",
                "x5l83yy675",
                "x8jxxk4ff5",
                "x8uqf25wch",
                "x8yj322td6",
                "xga6mpmplv",
                "xmn954pzmp",
                "xx9sdjej2w",
                "xy9t38ct57",
                "y45688jllp",
                "y5ghdn5j9k",
                "y755zyxw56",
                "yclnxrl5pm",
                "ydx93a7ass",
                "yrqqpx2mcb",
                "z24wtl6j62",
                "z4gj7hsk7h",
                "z5b3gh5ugf",
                "z959bm4gru",
                "zh3b7bxvad",
                "zmvfpc5aq8",
                "zq4921623r",
                "zq492l623r"
            };

            var arraySKAdNetworkItems = plist.root.CreateArray("SKAdNetworkItems");

            foreach (var id in ids)
            {
                var dictSKAdNetworkIdentifier_FAN = arraySKAdNetworkItems.AddDict();
                dictSKAdNetworkIdentifier_FAN.SetString("SKAdNetworkIdentifier", $"{id}.skadnetwork");
            }

            File.WriteAllText(plistPath, plist.WriteToString());

            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            var targetGuid = project.GetUnityMainTargetGuid();
            project.AddCapability(targetGuid, PBXCapabilityType.InAppPurchase);
            project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");

            var frameworkguid = project.GetUnityFrameworkTargetGuid();
            project.SetBuildProperty(frameworkguid, "ENABLE_BITCODE", "NO");
            project.SetBuildProperty(frameworkguid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

            string bridgingHeaderFileName = "Unity-iPhone-Bridging-Header.h";
            //string bridgingHeaderFilePath = Path.Combine(buildPath,"Libraries", "Plugins", "iOS", bridgingHeaderFileName);
            project.SetBuildProperty(targetGuid, "SWIFT_OBJC_BRIDGING_HEADER", bridgingHeaderFileName);
        
            //var fileGuid = project.AddFile(Path.Combine(buildPath, "GoogleService-Info.plist"), Path.Combine(buildPath, "GoogleService-Info.plist"));
            //project.AddFileToBuild(targetGuid, fileGuid);

            var appNames = AssetDatabase.LoadAssetAtPath<AppNameLocalize>("Assets/BanpoFri/Editor/AppNameLocalize.asset");
            var localizations = new (string Code, string DisplayName, string TrackingDescription)[]
            {
                ("en", appNames?.en, "This uses device info for more personalized ads and content"),
                ("ko", appNames?.ko, "보다 개인화된 광고 및 콘텐츠를 위해 기기 정보를 사용합니다."),
                ("ja", appNames?.ja, "これはユーザーデータをもとに、より関連性の高い広告コンテンツをお客様に提供します"),
                ("de", appNames?.de, "Dies benutzt Gerätinformationen für relevantere Werbeinhalte"),
                ("es", appNames?.es, "Esto utiliza la información del dispositivo para anuncios y contenido más personalizados"),
                ("fr", appNames?.fr, "Cela permet d'utiliser les informations du téléphone pour afficher des contenus publicitaires plus pertinents."),
                ("zh-Hans", appNames?.zhHans, "我们使用设备信息来提供个性化的广告和内容。"),
                ("zh-Hant", appNames?.zhHant, "我們使用設備信息來提供個性化的廣告和內容。")
            };

            foreach (var localization in localizations)
            {
                LocalizeInfoPlist(localization.Code, localization.DisplayName, localization.TrackingDescription,
                    buildPath, project, targetGuid);
            }

            project.WriteToFile(projectPath);

            // get entitlements path
            string[] idArray = Application.identifier.Split('.');
            var entitlementsPath = $"Unity-iPhone/{idArray[idArray.Length - 1]}.entitlements";

            // create capabilities manager
            var capManager = new ProjectCapabilityManager(projectPath, entitlementsPath, null, targetGuid);

            // Add necessary capabilities
            capManager.AddPushNotifications(true);
            capManager.AddSignInWithApple();

            // Write to file
            capManager.WriteToFile();

            var projectInString = File.ReadAllText(projectPath);

            projectInString = projectInString.Replace("ENABLE_BITCODE = YES;",
                $"ENABLE_BITCODE = NO;");
            File.WriteAllText(projectPath, projectInString);
        }

        private static void LocalizeInfoPlist(string localeCode, string displayName, string trackingDescription,
            string buildPath, PBXProject project, string targetGuid)
        {
            var resourcesDirectoryName = Directory.Exists(Path.Combine(buildPath, LegacyResourcesDirectoryName))
                ? LegacyResourcesDirectoryName
                : AppLovinMaxResourcesDirectoryName;
            var localeDirectory = Path.Combine(resourcesDirectoryName, localeCode + ".lproj");
            var localeDirectoryPath = Path.Combine(buildPath, localeDirectory);
            Directory.CreateDirectory(localeDirectoryPath);

            var stringsPath = Path.Combine(localeDirectoryPath, "InfoPlist.strings");
            var lines = File.Exists(stringsPath)
                ? new List<string>(File.ReadAllLines(stringsPath))
                : new List<string> { "/* Localized versions of Info.plist keys - Generated by iOS post-process */" };
            if (!string.IsNullOrEmpty(displayName))
                SetLocalizedInfoPlistValue(lines, "CFBundleDisplayName", displayName);
            SetLocalizedInfoPlistValue(lines, "NSUserTrackingUsageDescription", trackingDescription);
            File.WriteAllLines(stringsPath, lines);

            var guid = project.AddFolderReference(localeDirectory, localeDirectory);
            project.AddFileToBuild(targetGuid, guid);
        }

        private static void SetLocalizedInfoPlistValue(List<string> lines, string key, string value)
        {
            var quotedKey = "\"" + key + "\"";
            var escapedValue = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var entry = quotedKey + " = \"" + escapedValue + "\";";
            var index = lines.FindIndex(line => line.TrimStart().StartsWith(quotedKey, System.StringComparison.Ordinal));
            if (index >= 0)
                lines[index] = entry;
            else
                lines.Add(entry);
        }
    }
}

#endif