/*
    Copyright (c) 2011 Alastair Paterson

    Permission is hereby granted, free of charge, to any person obtaining a copy of this
    software and associated documentation files (the "Software"), to deal in the Software
    without restriction, including without limitation the rights to use, copy, modify,
    merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be included in all copies
    or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
    OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParseTests
{
    /// <summary>
    /// Summary description for Tests
    /// </summary>
    [TestClass]
    public class Tests
    {
        public Parse.ParseClient localClient;
        public Tests()
        {
            localClient = new Parse.ParseClient("CsQUidbvlr7hxU6KAScTXZfri7RCUxupK6kxmLvy", "h2W1KKwr3daS3oY8NFvP6KPrmMmPFoNnDILnWG9Y");
            //Uses TestApp
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            Parse.ParseObject testObject = new Parse.ParseObject("ClassOne");
            testObject["foo"] = "bar";
            //Create a new object
            testObject = localClient.CreateObject(testObject);

            //Test to make sure we returned a ParseObject
            Assert.IsNotNull(testObject);
            //Test to make sure we were assigned an object id and the object was actually remotely created
            Assert.IsNotNull(testObject.objectId);

            //Search for the newly-created object on the server
            Parse.ParseObject searchObject = localClient.GetObject("ClassOne", testObject.objectId);
            //Test to make sure the same object was returned
            Assert.AreEqual(testObject.objectId, searchObject.objectId);

            testObject["foo"] = "notbar";
            //Change a value on the server
            localClient.UpdateObject(testObject);

            searchObject = localClient.GetObject("ClassOne", testObject.objectId);
            //Test to make sure the object was updated on the server
            Assert.AreEqual("notbar", searchObject["foo"]);

            //Test to make sure we can retrieve objects from Parse
            Parse.ParseObject[]objList = localClient.GetObjectsWithQuery("ClassOne", new { foo = "notbar" });
            Assert.IsNotNull(objList);

            //Test to make sure the same object was returned
            Assert.AreEqual(objList.First()["objectId"], testObject.objectId);

            //Cleanup
            localClient.DeleteObject(testObject);
        }
    }
}
