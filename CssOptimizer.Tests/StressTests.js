

var stresTestModule = (function () {
    return {
        config: {
            baseApiUrl: "http://localhost:56587/api",
            BrowserOptmimizePath: "/v1/optimize/css",
            BrowserOptmimizeInParallelPath: "/v1/optimize/css/parallel",
            CustomOptmimizePath: "/v2/optimize/css",
            CustomOptmimizeInParallelPath: "/v2/optimize/css/parallel",

            CacheResetPath: "/cache/reset"
        },

        ResetCache: function () {
            var xmlHttp = new XMLHttpRequest();
            xmlHttp.open("DELETE", stresTestModule.config.baseApiUrl + stresTestModule.config.CacheResetPath, false); // false for synchronous request
            xmlHttp.send(null);
        },

        RunOptimize: function (urls, apiMethod) {
            if (!urls || urls.length === 0) {
                console.warn("urls are empty.");
            }

            var times = [];

            urls.forEach(function (url, i) {

                var startTime = new Date();

                var xmlHttp = new XMLHttpRequest();
                xmlHttp.open("GET", stresTestModule.config.baseApiUrl + apiMethod + "?url=" + url, false); // false for synchronous request
                xmlHttp.send(null);

                var endTime = new Date();
                var timeDifference = (endTime - startTime) / 1000;
                times.push(timeDifference);

                console.log(`[${apiMethod}] Calculated min CSS for ${url} for ${timeDifference} second(s).`);

            });

            const average = arrAvg(times);

            console.log(`Average time is ${average} second(s).`);
        },

        RunOptimizeInParallel: function (urls, apiMethod) {
            if (!urls || urls.length === 0) {
                console.warn("urls are empty.");
            }

            var startTime = new Date();

            var xmlHttp = new XMLHttpRequest();
            xmlHttp.open("POST", stresTestModule.config.baseApiUrl + apiMethod, false); // false for synchronous request
            xmlHttp.setRequestHeader("Content-Type", "application/json");
            xmlHttp.send(JSON.stringify(urls));

            var endTime = new Date();
            var timeDifference = (endTime - startTime) / 1000;

            console.log(`[${apiMethod}] Calculated min CSS for ${urls} for ${timeDifference} second(s).`);
        }
    };
}());

var testRunner = (function () {
    return {
        run: function () {

            var urls = [
                "https://pikabu.ru/",
                "https://www.privat24.ua/#login",
                "https://mail.google.com/mail/u/0/#search/has%3Anouserlabels",
                "https://www.freelancer.com",
                "http://olx.ua",
                "http://youtube.com",
                "https://outlook.live.com/owa/",
                "http://staff-clothes.com"
            ];

            //CUSTOM ALOGRITHM:
            console.log("CUSTOM ALOGRITHM:");
            stresTestModule.RunOptimize(urls, stresTestModule.config.CustomOptmimizePath);
            stresTestModule.ResetCache();

            stresTestModule.RunOptimizeInParallel(urls, stresTestModule.config.CustomOptmimizeInParallelPath);
            stresTestModule.ResetCache();

            // GOOGLE CHROME Approach:

            console.log("GOOGLE CHROME Approach:");
            stresTestModule.RunOptimize(urls, stresTestModule.config.BrowserOptmimizePath);
            stresTestModule.ResetCache();

            stresTestModule.RunOptimizeInParallel(urls, stresTestModule.config.BrowserOptmimizeInParallelPath);
            stresTestModule.ResetCache();
        }
    };
}());

const arrAvg = arr => arr.reduce((a, b) => a + b, 0) / arr.length