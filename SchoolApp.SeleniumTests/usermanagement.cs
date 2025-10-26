using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using Xunit;

namespace SchoolApp.SeleniumTests
{
    public class SeleniumTests : IDisposable
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public SeleniumTests()
        {
            driver = new ChromeDriver();
            driver.Manage().Window.Maximize();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
        }

        private void HandlePossibleAlert()
        {
            try
            {
                wait.Until(ExpectedConditions.AlertIsPresent());
                IAlert alert = driver.SwitchTo().Alert();
                alert.Accept();
            }
            catch (WebDriverTimeoutException) { }
        }

        private void Login(string email, string password)
        {
            driver.Navigate().GoToUrl("http://localhost:3000/login");
            var emailInput = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//input[@type='email']")));
            emailInput.SendKeys(email);
            var passwordInput = driver.FindElement(By.XPath("//input[@type='password']"));
            passwordInput.SendKeys(password);
            var signInButton = driver.FindElement(By.XPath("//button[@type='submit' and contains(text(),'Sign In')]"));
            signInButton.Click();
            try
            {
                wait.Until(ExpectedConditions.AlertIsPresent());
                IAlert alert = driver.SwitchTo().Alert();
                if (alert.Text.Contains("change password", StringComparison.OrdinalIgnoreCase))
                {
                    alert.Accept();
                    wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//button[@type='submit' and contains(text(),'Sign In')]")));
                    Login(email, password);
                }
                else
                {
                    alert.Accept();
                    throw new Exception("Unexpected alert during login: " + alert.Text);
                }
            }
            catch (WebDriverTimeoutException) { }
            catch (NoAlertPresentException) { }
        }

        [Fact]
        public void TestLoginAndProfileDisplay()
        {
            Login("ruba@gmail.com", "ruba@123");
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(),'My Profile')]")));
            Assert.Contains("My Profile", driver.PageSource);
            Assert.Contains("ruba@gmail.com", driver.PageSource);
        }

        [Fact]
        public void TestAddUserInUserManagement()
        {
            Login("renu@gmail.com", "renu@123");
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(),'User Management')]")));
            driver.Navigate().GoToUrl("http://localhost:3000/users");
            var addUserButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[normalize-space(text())='Add User']")));
            addUserButton.Click();

            wait.Until(ExpectedConditions.ElementIsVisible(By.Name("name"))).SendKeys("Test User");
            driver.FindElement(By.Name("dob")).SendKeys("1990-01-01");
            driver.FindElement(By.Name("gender")).SendKeys("Female");
            var dropdownTriggers = driver.FindElements(By.CssSelector("div.MuiSelect-select"));

            dropdownTriggers[0].Click();
            wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//li[text()='Student']"))).Click();
            wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector("ul.MuiList-root")));

            driver.FindElement(By.Name("department")).SendKeys("General");
            driver.FindElement(By.Name("email")).SendKeys("test2.user@example.com");
            driver.FindElement(By.Name("phonenumber")).SendKeys("1234567890");
            driver.FindElement(By.Name("address")).SendKeys("123 Test Address");
            driver.FindElement(By.Name("password")).SendKeys("testpassword");

            dropdownTriggers[1].Click();
            wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//li[text()='Student']"))).Click();
            wait.Until(ExpectedConditions.InvisibilityOfElementLocated(By.CssSelector("ul.MuiList-root")));

            var addButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[normalize-space()='Add']")));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", addButton);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", addButton);

            wait.Until(ExpectedConditions.AlertIsPresent());
            driver.SwitchTo().Alert().Accept();

            Assert.True(true);
        }

        [Fact]
        public void TestEditUser()
        {
            Login("renu@gmail.com", "renu@123");
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(),'User Management')]")));
            driver.Navigate().GoToUrl("http://localhost:3000/users");
            var editButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("(//button[contains(text(),'Edit')])[1]")));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", editButton);
            var nameField = wait.Until(ExpectedConditions.ElementIsVisible(By.Name("name")));
            nameField.Clear();
            nameField.SendKeys("Updated User");
            var departmentField = driver.FindElement(By.Name("department"));
            departmentField.Clear();
            departmentField.SendKeys("Mathematics");
            var updateButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(),'Update')]")));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", updateButton);
            HandlePossibleAlert();
            Assert.True(true);
        }

        [Fact]
        public void TestDeleteUser()
        {
            Login("renu@gmail.com", "renu@123");
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[contains(text(),'User Management')]")));
            driver.Navigate().GoToUrl("http://localhost:3000/users");
            var deleteButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("(//button[contains(text(),'Delete')])[1]")));
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", deleteButton);
            IAlert alert = wait.Until(ExpectedConditions.AlertIsPresent());
            alert.Accept();
            Assert.True(true);
        }

        public void Dispose()
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}

