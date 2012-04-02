/*
    Copyright (c) 2011 Ricardo J. Méndez

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
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.IO;

namespace ParseTests
{
    /// <summary>
    /// Straightforward API tests to both ensure that the API works and 
    /// help as examples.
    /// </summary>
    [TestFixture]
    public class BasicAPITests
    {
        Parse.ParseClient localClient;
		Random random;
		
		
        public BasicAPITests()
        {
            localClient = new Parse.ParseClient("CsQUidbvlr7hxU6KAScTXZfri7RCUxupK6kxmLvy", "h2W1KKwr3daS3oY8NFvP6KPrmMmPFoNnDILnWG9Y");
			random = new Random();
        }
		
		[Test]
		public void TestCreateAndQuery()
		{
            var testObject = new Parse.ParseObject("Score");
			var score = random.Next();
            testObject["Level"] = "First";
			testObject["Score"] = score;
            //Create a new object
            testObject = localClient.CreateObject(testObject);
			
            //Test to make sure we returned a ParseObject
            Assert.IsNotNull(testObject);
            //Test to make sure we were assigned an object id and the object was actually remotely created
            Assert.IsNotNull(testObject.objectId);

            //Test to make sure we can retrieve objects from Parse
            Parse.ParseObject[] objList = localClient.GetObjectsWithQuery("Score", new { Level = "First" });
            Assert.IsNotNull(objList);
			var foundId = objList.First(x => (string)x["objectId"] == testObject.objectId);
			Assert.IsNotNull(foundId);
			Assert.AreEqual((Int64)foundId["Score"], score);	
		}
		
		
		[Test]
		public void TestDeletion()
		{
			var watch = new Stopwatch();
            var testObject = new Parse.ParseObject("Score");
			var score = random.Next();
            testObject["Level"] = "First";
			testObject["Score"] = score;
            //Create a new object
			watch.Restart();
            testObject = localClient.CreateObject(testObject);
			watch.Stop();
			Console.WriteLine(string.Format("Individual creation took {0}ms", watch.ElapsedMilliseconds));
			
            var objList = localClient.GetObjectsWithQuery("Score", new { Level = "First", objectId = testObject.objectId });
            Assert.IsNotNull(objList);
			Assert.AreEqual(objList.Length, 1);
			
			watch.Restart();
			localClient.DeleteObject(testObject);			
			watch.Stop();
			Console.WriteLine(string.Format("Individual deletion took {0}ms", watch.ElapsedMilliseconds));
			
			objList = localClient.GetObjectsWithQuery("Score", new { Level = "First", objectId = testObject.objectId });
            Assert.IsEmpty(objList);
		}
		
		[Test]
		public void TestQueryLimitsAndSorting()
		{
			var watch = new Stopwatch();
			
			Parse.ParseObject[] objects = new Parse.ParseObject[20];
			
			var levelName = string.Format("Level-{0}", random.Next());
			
			Console.WriteLine("Creating...");
			watch.Restart();
			for (int i = 0; i < 20; i++)
			{
				var parseObject = new Parse.ParseObject("LevelScore");
				parseObject["Level"] = levelName;
				parseObject["Score"] = i;
				objects[i] = localClient.CreateObject(parseObject);
				Assert.IsNotNull(objects[i].objectId);
			}
			watch.Stop();
			Console.WriteLine(string.Format("Creation took {0}ms", watch.ElapsedMilliseconds));
			
			// Retrieve the first 10 scores for this level
			Console.WriteLine("Retrieving...");
			watch.Restart();
			var objList = localClient.GetObjectsWithQuery("LevelScore", new { Level = levelName }, "Score", 10);
			watch.Stop();
			Console.WriteLine(string.Format("Retrieval took {0}ms", watch.ElapsedMilliseconds));
			Assert.IsNotEmpty(objList);
			Assert.AreEqual(objList.Length, 10);
			
			// All scores returned should be in the 0-9 range, in ascending order
			for (int i = 0; i <= 9; i++)
			{
				Assert.AreEqual((string)objList[i]["Level"], levelName);
				Assert.AreEqual((Int64)objList[i]["Score"], i);
			}
			
			
			// Retrieve the middle 10 scores for this level, in inverse order
			Console.WriteLine("Retrieving second batch...");
			watch.Restart();
			objList = localClient.GetObjectsWithQuery("LevelScore", new { Level = levelName }, "-Score", 10, 5);
			watch.Stop();
			Console.WriteLine(string.Format("Sorted retrieval took {0}ms", watch.ElapsedMilliseconds));
			Assert.IsNotEmpty(objList);
			Assert.AreEqual(objList.Length, 10);
			
			// All scores returned should be in the 0-9 range, in ascending order
			for (int i = 0; i <= 9; i++)
			{
				Assert.AreEqual((string)objList[i]["Level"], levelName);
				Assert.AreEqual((Int64)objList[i]["Score"], 14-i);
			}
			
			// Retrieve the top 5 scores for this level, in inverse order
			Console.WriteLine("Retrieving third batch...");
			objList = localClient.GetObjectsWithQuery("LevelScore", new { Level = levelName }, "-Score", 5, 0);
			Assert.IsNotEmpty(objList);
			Assert.AreEqual(objList.Length, 5);
			
			// All scores returned should be in the 0-9 range, in ascending order
			for (int i = 0; i <= 4; i++)
			{
				Assert.AreEqual((string)objList[i]["Level"], levelName);
				Assert.AreEqual((Int64)objList[i]["Score"], 19-i);
			}
			
			
			// Let's clean up
			Console.WriteLine("Cleaning up...");
			watch.Restart();
			foreach(var o in objects)
			{
				localClient.DeleteObject(o);
			}
			watch.Stop();
			Console.WriteLine(string.Format("Clean up took {0}ms", watch.ElapsedMilliseconds));
		}
		
		[Test]
		public void TestGetObject()
		{
			Parse.ParseObject[] objects = new Parse.ParseObject[20];
			
			var levelName = string.Format("Level-{0}", random.Next());
			
			Console.WriteLine("Creating...");
			for (int i = 0; i < 20; i++)
			{
				var parseObject = new Parse.ParseObject("LevelScore");
				parseObject["Level"] = levelName;
				parseObject["Score"] = i;
				objects[i] = localClient.CreateObject(parseObject);
				Assert.IsNotNull(objects[i].objectId);
			}

			
			// Retrieve the first 10 scores for this level
			Console.WriteLine("Retrieving...");
			foreach(var o in objects)
			{
				var parseObject = localClient.GetObject("LevelScore", o.objectId);
				
				Assert.IsNotNull(parseObject);
				Assert.AreEqual(o["Level"], parseObject["Level"]);
				Assert.AreEqual(o["Score"], parseObject["Score"]);
			}
			
			
			// Let's clean up
			Console.WriteLine("Cleaning up...");
			foreach(var o in objects)
			{
				localClient.DeleteObject(o);
			}
		}
		
    }
}
