﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Edge;
using System;
using WindowsInput;
using WindowsInput.Native;
using System.Threading;
using System.Linq;

namespace Microsoft.PowerApps.TestAutomation.Browser
{
    public static class BrowserDriverFactory
    {
        public static IWebDriver CreateWebDriver(BrowserOptions options)
        {
            IWebDriver driver;
           
            switch (options.BrowserType)
            {
                case BrowserType.Chrome:
                    var chromeService = ChromeDriverService.CreateDefaultService(options.DriversPath);
                    chromeService.HideCommandPromptWindow = options.HideDiagnosticWindow;
                    driver = new ChromeDriver(chromeService, options.ToChrome());
                    BrowserDriverFactory.EnableThirdPartyCookies(driver);
                    break;
                case BrowserType.Firefox:
                    var ffService = FirefoxDriverService.CreateDefaultService(options.DriversPath);
                    ffService.HideCommandPromptWindow = options.HideDiagnosticWindow;
                    driver = new FirefoxDriver(ffService);
                    driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 5);
                    break;
                case BrowserType.Edge:
                    var edgeService = EdgeDriverService.CreateDefaultService(options.DriversPath);
                    edgeService.HideCommandPromptWindow = options.HideDiagnosticWindow;
                    driver = new EdgeDriver(edgeService,options.ToEdge(), TimeSpan.FromMinutes(20));

                    break;
                default:
                    throw new InvalidOperationException(
                        $"The browser type '{options.BrowserType}' is not recognized.");
            }

            driver.Manage().Timeouts().PageLoad = options.PageLoadTimeout;

            if(options.StartMaximized && options.BrowserType != BrowserType.Chrome) //Handle Chrome in the Browser Options
                driver.Manage().Window.Maximize();

            if (options.FireEvents || options.EnableRecording)
            {
                // Wrap the newly created driver.
                driver = new EventFiringWebDriver(driver);
            }

            return driver;
        }

        private static void EnableThirdPartyCookies(this IWebDriver driver)
        {
            var windowHandles = driver.WindowHandles;
            
            // Activate Browser window
            driver.SwitchTo().Window(driver.WindowHandles.Last());

            // Open New Tab Ctrl + T    
            new InputSimulator().Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);

            // Wait for open new tab
            const int retries = 100;
            for (var i = 0; i < retries; i++)
            {
                Thread.Sleep(100);
                if (driver.WindowHandles.Count > windowHandles.Count)
                    break;
            }

            // Enable Third Party Cookies
            if (driver.WindowHandles.Count > windowHandles.Count)
            {
                driver.Close();
                driver.SwitchTo().Window(driver.WindowHandles.Last());
                var selectedCookieControlsToggle = driver.FindElements(By.Id("cookie-controls-toggle"))
                    .FirstOrDefault(x => x.GetAttribute("checked") != null);
                selectedCookieControlsToggle?.Click();
            }
        }
    }
}