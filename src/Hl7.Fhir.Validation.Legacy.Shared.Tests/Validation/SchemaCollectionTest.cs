﻿/* 
 * Copyright (c) 2014, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Hl7.Fhir.Specification.Tests
{
    [TestClass]
    public class SchemaCollectionTest
    {
        [TestMethod]
        public void TestSchemaCompilation()
        {
            var schemas = SchemaCollection.ValidationSchemaSet;
            Assert.IsTrue(schemas.Count > 0);

            var patDoc = XDocument.Parse("<Patient xmlns='http://hl7.org/fhir' />");
            patDoc.Validate(SchemaCollection.ValidationSchemaSet, null);

            try
            {
                patDoc = XDocument.Parse("<Patient xmlns='http://hl7.org/fhir'><garbage/></Patient>");
                patDoc.Validate(SchemaCollection.ValidationSchemaSet, null);
                Assert.Fail();
            }
            catch
            {
                // perfect.
            }

            var hasError = false;
            patDoc = XDocument.Parse("<Patient xmlns='http://hl7.org/fhir'><garbage/></Patient>");
            patDoc.Validate(SchemaCollection.ValidationSchemaSet, (source, args) => hasError = true);
            Assert.IsTrue(hasError);
        }

        [TestMethod]
        public void TestSchemaCollectionValidation()
        {
            var s = File.ReadAllText(Path.Combine("TestData", "TestPatient.xml"));
            var doc = SerializationUtil.XDocumentFromXmlText(s);

            string message = null;

            doc.Validate(SchemaCollection.ValidationSchemaSet, (source, args) => message = args.Message);

            Debug.WriteLine(message);
            Assert.IsNull(message);
        }

    }
}