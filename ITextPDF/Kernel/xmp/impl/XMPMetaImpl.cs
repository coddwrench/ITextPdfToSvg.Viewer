//Copyright (c) 2006, Adobe Systems Incorporated
//All rights reserved.
//
//        Redistribution and use in source and binary forms, with or without
//        modification, are permitted provided that the following conditions are met:
//        1. Redistributions of source code must retain the above copyright
//        notice, this list of conditions and the following disclaimer.
//        2. Redistributions in binary form must reproduce the above copyright
//        notice, this list of conditions and the following disclaimer in the
//        documentation and/or other materials provided with the distribution.
//        3. All advertising materials mentioning features or use of this software
//        must display the following acknowledgement:
//        This product includes software developed by the Adobe Systems Incorporated.
//        4. Neither the name of the Adobe Systems Incorporated nor the
//        names of its contributors may be used to endorse or promote products
//        derived from this software without specific prior written permission.
//
//        THIS SOFTWARE IS PROVIDED BY ADOBE SYSTEMS INCORPORATED ''AS IS'' AND ANY
//        EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//        DISCLAIMED. IN NO EVENT SHALL ADOBE SYSTEMS INCORPORATED BE LIABLE FOR ANY
//        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//        http://www.adobe.com/devnet/xmp/library/eula-xmp-library-java.html

using System;
using System.Diagnostics;
using IText.Kernel.XMP.Impl.XPath;
using IText.Kernel.XMP.Options;
using IText.Kernel.XMP.Properties;

namespace IText.Kernel.XMP.Impl
{
	/// <summary>
	/// Implementation for
	/// <see cref="XMPMeta"/>
	/// .
	/// </summary>
	/// <since>17.02.2006</since>
	public class XMPMetaImpl : XMPConst, XMPMeta
	{
		/// <summary>Property values are Strings by default</summary>
		private const int VALUE_STRING = 0;

		private const int VALUE_BOOLEAN = 1;

		private const int VALUE_INTEGER = 2;

		private const int VALUE_LONG = 3;

		private const int VALUE_DOUBLE = 4;

		private const int VALUE_DATE = 5;

		private const int VALUE_CALENDAR = 6;

		private const int VALUE_BASE64 = 7;

		/// <summary>root of the metadata tree</summary>
		private XMPNode tree;

		/// <summary>the xpacket processing instructions content</summary>
		private string packetHeader;

		/// <summary>Constructor for an empty metadata object.</summary>
		public XMPMetaImpl()
		{
			// create root node
			tree = new XMPNode(null, null, null);
		}

		/// <summary>Constructor for a cloned metadata tree.</summary>
		/// <param name="tree">
		/// an prefilled metadata tree which fulfills all
		/// <code>XMPNode</code> contracts.
		/// </param>
		public XMPMetaImpl(XMPNode tree)
		{
			this.tree = tree;
		}

		public virtual void AppendArrayItem(string schemaNS, string arrayName, PropertyOptions
			 arrayOptions, string itemValue, PropertyOptions itemOptions)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(arrayName);
			if (arrayOptions == null)
			{
				arrayOptions = new PropertyOptions();
			}
			if (!arrayOptions.IsOnlyArrayOptions())
			{
				throw new XMPException("Only array form flags allowed for arrayOptions", XMPError
					.BADOPTIONS);
			}
			// Check if array options are set correctly.
			arrayOptions = XMPNodeUtils.VerifySetOptions(arrayOptions, null);
			// Locate or create the array. If it already exists, make sure the array
			// form from the options
			// parameter is compatible with the current state.
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, arrayName);
			// Just lookup, don't try to create.
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, false, null);
			if (arrayNode != null)
			{
				// The array exists, make sure the form is compatible. Zero
				// arrayForm means take what exists.
				if (!arrayNode.GetOptions().IsArray())
				{
					throw new XMPException("The named property is not an array", XMPError.BADXPATH);
				}
			}
			else
			{
				// if (arrayOptions != null && !arrayOptions.equalArrayTypes(arrayNode.getOptions()))
				// {
				// throw new XMPException("Mismatch of existing and specified array form", BADOPTIONS);
				// }
				// The array does not exist, try to create it.
				if (arrayOptions.IsArray())
				{
					arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, true, arrayOptions);
					if (arrayNode == null)
					{
						throw new XMPException("Failure creating array node", XMPError.BADXPATH);
					}
				}
				else
				{
					// array options missing
					throw new XMPException("Explicit arrayOptions required to create new array", XMPError
						.BADOPTIONS);
				}
			}
			DoSetArrayItem(arrayNode, ARRAY_LAST_ITEM, itemValue, itemOptions, true);
		}

		public virtual void AppendArrayItem(string schemaNS, string arrayName, string itemValue
			)
		{
			AppendArrayItem(schemaNS, arrayName, null, itemValue, null);
		}

		public virtual int CountArrayItems(string schemaNS, string arrayName)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(arrayName);
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, arrayName);
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, false, null);
			if (arrayNode == null)
			{
				return 0;
			}
			if (arrayNode.GetOptions().IsArray())
			{
				return arrayNode.GetChildrenLength();
			}

			throw new XMPException("The named property is not an array", XMPError.BADXPATH);
		}

		public virtual void DeleteArrayItem(string schemaNS, string arrayName, int itemIndex
			)
		{
			try
			{
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertArrayName(arrayName);
				var itemPath = XMPPathFactory.ComposeArrayItemPath(arrayName, itemIndex);
				DeleteProperty(schemaNS, itemPath);
			}
			catch (XMPException)
			{
				// EMPTY, exceptions are ignored within delete
			}
		}

		public virtual void DeleteProperty(string schemaNS, string propName)
		{
			try
			{
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertPropName(propName);
				var expPath = XMPPathParser.ExpandXPath(schemaNS, propName);
				var propNode = XMPNodeUtils.FindNode(tree, expPath, false, null);
				if (propNode != null)
				{
					XMPNodeUtils.DeleteNode(propNode);
				}
			}
			catch (XMPException)
			{
				// EMPTY, exceptions are ignored within delete
			}
		}

		public virtual void DeleteQualifier(string schemaNS, string propName, string qualNS
			, string qualName)
		{
			try
			{
				// Note: qualNS and qualName are checked inside composeQualfierPath
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertPropName(propName);
				var qualPath = propName + XMPPathFactory.ComposeQualifierPath(qualNS, qualName
					);
				DeleteProperty(schemaNS, qualPath);
			}
			catch (XMPException)
			{
				// EMPTY, exceptions within delete are ignored
			}
		}

		public virtual void DeleteStructField(string schemaNS, string structName, string 
			fieldNS, string fieldName)
		{
			try
			{
				// fieldNS and fieldName are checked inside composeStructFieldPath
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertStructName(structName);
				var fieldPath = structName + XMPPathFactory.ComposeStructFieldPath(fieldNS, fieldName
					);
				DeleteProperty(schemaNS, fieldPath);
			}
			catch (XMPException)
			{
				// EMPTY, exceptions within delete are ignored
			}
		}

		
		public virtual bool DoesPropertyExist(string schemaNS, string propName)
		{
			try
			{
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertPropName(propName);
				var expPath = XMPPathParser.ExpandXPath(schemaNS, propName);
				var propNode = XMPNodeUtils.FindNode(tree, expPath, false, null);
				return propNode != null;
			}
			catch (XMPException)
			{
				return false;
			}
		}

		public virtual bool DoesArrayItemExist(string schemaNS, string arrayName, int itemIndex
			)
		{
			try
			{
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertArrayName(arrayName);
				var path = XMPPathFactory.ComposeArrayItemPath(arrayName, itemIndex);
				return DoesPropertyExist(schemaNS, path);
			}
			catch (XMPException)
			{
				return false;
			}
		}

		public virtual bool DoesStructFieldExist(string schemaNS, string structName, string
			 fieldNS, string fieldName)
		{
			try
			{
				// fieldNS and fieldName are checked inside composeStructFieldPath()
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertStructName(structName);
				var path = XMPPathFactory.ComposeStructFieldPath(fieldNS, fieldName);
				return DoesPropertyExist(schemaNS, structName + path);
			}
			catch (XMPException)
			{
				return false;
			}
		}

		public virtual bool DoesQualifierExist(string schemaNS, string propName, string qualNS
			, string qualName)
		{
			try
			{
				// qualNS and qualName are checked inside composeQualifierPath()
				ParameterAsserts.AssertSchemaNS(schemaNS);
				ParameterAsserts.AssertPropName(propName);
				var path = XMPPathFactory.ComposeQualifierPath(qualNS, qualName);
				return DoesPropertyExist(schemaNS, propName + path);
			}
			catch (XMPException)
			{
				return false;
			}
		}

		public virtual XMPProperty GetArrayItem(string schemaNS, string arrayName, int itemIndex
			)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(arrayName);
			var itemPath = XMPPathFactory.ComposeArrayItemPath(arrayName, itemIndex);
			return GetProperty(schemaNS, itemPath);
		}

		public virtual XMPProperty GetLocalizedText(string schemaNS, string altTextName, 
			string genericLang, string specificLang)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(altTextName);
			ParameterAsserts.AssertSpecificLang(specificLang);
			genericLang = genericLang != null ? Utils.NormalizeLangValue
				(genericLang) : null;
			specificLang = Utils.NormalizeLangValue(specificLang);
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, altTextName);
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, false, null);
			if (arrayNode == null)
			{
				return null;
			}
			var result = XMPNodeUtils.ChooseLocalizedText(arrayNode, genericLang, specificLang
				);
			var match = (int)result[0];
			var itemNode = (XMPNode)result[1];
			if (match != XMPNodeUtils.CLT_NO_VALUES)
			{
				return new _XMPProperty_428(itemNode);
			}

			return null;
		}

		private sealed class _XMPProperty_428 : XMPProperty
		{
			public _XMPProperty_428(XMPNode itemNode)
			{
				this.itemNode = itemNode;
			}

			public string GetValue()
			{
				return itemNode.GetValue();
			}

			public PropertyOptions GetOptions()
			{
				return itemNode.GetOptions();
			}

			public string GetLanguage()
			{
				return itemNode.GetQualifier(1).GetValue();
			}

			public override string ToString()
			{
				return itemNode.GetValue();
			}

			private readonly XMPNode itemNode;
		}

		public virtual void SetLocalizedText(string schemaNS, string altTextName, string 
			genericLang, string specificLang, string itemValue, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(altTextName);
			ParameterAsserts.AssertSpecificLang(specificLang);
			genericLang = genericLang != null ? Utils.NormalizeLangValue
				(genericLang) : null;
			specificLang = Utils.NormalizeLangValue(specificLang);
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, altTextName);
			// Find the array node and set the options if it was just created.
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, true, new PropertyOptions
				(PropertyOptions.ARRAY | PropertyOptions.ARRAY_ORDERED | PropertyOptions.ARRAY_ALTERNATE
				 | PropertyOptions.ARRAY_ALT_TEXT));
			if (arrayNode == null)
			{
				throw new XMPException("Failed to find or create array node", XMPError.BADXPATH);
			}

			if (!arrayNode.GetOptions().IsArrayAltText())
			{
				if (!arrayNode.HasChildren() && arrayNode.GetOptions().IsArrayAlternate())
				{
					arrayNode.GetOptions().SetArrayAltText(true);
				}
				else
				{
					throw new XMPException("Specified property is no alt-text array", XMPError.BADXPATH
					);
				}
			}
			// Make sure the x-default item, if any, is first.
			var haveXDefault = false;
			XMPNode xdItem = null;
			foreach (XMPNode currItem in arrayNode.GetChildren())
			{
				if (!currItem.HasQualifier() || !XML_LANG.Equals(currItem.GetQualifier(1
					).GetName()))
				{
					throw new XMPException("Language qualifier must be first", XMPError.BADXPATH);
				}

				if (X_DEFAULT.Equals(currItem.GetQualifier(1).GetValue()))
				{
					xdItem = currItem;
					haveXDefault = true;
					break;
				}
			}
			// Moves x-default to the beginning of the array
			if (xdItem != null && arrayNode.GetChildrenLength() > 1)
			{
				arrayNode.RemoveChild(xdItem);
				arrayNode.AddChild(1, xdItem);
			}
			// Find the appropriate item.
			// chooseLocalizedText will make sure the array is a language
			// alternative.
			var result = XMPNodeUtils.ChooseLocalizedText(arrayNode, genericLang, specificLang
				);
			var match = (int)result[0];
			var itemNode = (XMPNode)result[1];
			var specificXDefault = X_DEFAULT.Equals(specificLang);
			switch (match)
			{
				case XMPNodeUtils.CLT_NO_VALUES:
				{
					// Create the array items for the specificLang and x-default, with
					// x-default first.
					XMPNodeUtils.AppendLangItem(arrayNode, X_DEFAULT, itemValue);
					haveXDefault = true;
					if (!specificXDefault)
					{
						XMPNodeUtils.AppendLangItem(arrayNode, specificLang, itemValue);
					}
					break;
				}

				case XMPNodeUtils.CLT_SPECIFIC_MATCH:
				{
					if (!specificXDefault)
					{
						// Update the specific item, update x-default if it matches the
						// old value.
						if (haveXDefault && xdItem != itemNode && xdItem != null && xdItem.GetValue().Equals
							(itemNode.GetValue()))
						{
							xdItem.SetValue(itemValue);
						}
						// ! Do this after the x-default check!
						itemNode.SetValue(itemValue);
					}
					else
					{
						// Update all items whose values match the old x-default value.
						Debug.Assert(haveXDefault && xdItem == itemNode);
						for (var it_1 = arrayNode.IterateChildren(); it_1.MoveNext(); )
						{
							var currItem = (XMPNode)it_1.Current;
							if (currItem == xdItem || !currItem.GetValue().Equals(xdItem != null ? xdItem.GetValue
								() : null))
							{
								continue;
							}
							currItem.SetValue(itemValue);
						}
						// And finally do the x-default item.
						if (xdItem != null)
						{
							xdItem.SetValue(itemValue);
						}
					}
					break;
				}

				case XMPNodeUtils.CLT_SINGLE_GENERIC:
				{
					// Update the generic item, update x-default if it matches the old
					// value.
					if (haveXDefault && xdItem != itemNode && xdItem != null && xdItem.GetValue().Equals
						(itemNode.GetValue()))
					{
						xdItem.SetValue(itemValue);
					}
					itemNode.SetValue(itemValue);
					// ! Do this after
					// the x-default
					// check!
					break;
				}

				case XMPNodeUtils.CLT_MULTIPLE_GENERIC:
				{
					// Create the specific language, ignore x-default.
					XMPNodeUtils.AppendLangItem(arrayNode, specificLang, itemValue);
					if (specificXDefault)
					{
						haveXDefault = true;
					}
					break;
				}

				case XMPNodeUtils.CLT_XDEFAULT:
				{
					// Create the specific language, update x-default if it was the only
					// item.
					if (xdItem != null && arrayNode.GetChildrenLength() == 1)
					{
						xdItem.SetValue(itemValue);
					}
					XMPNodeUtils.AppendLangItem(arrayNode, specificLang, itemValue);
					break;
				}

				case XMPNodeUtils.CLT_FIRST_ITEM:
				{
					// Create the specific language, don't add an x-default item.
					XMPNodeUtils.AppendLangItem(arrayNode, specificLang, itemValue);
					if (specificXDefault)
					{
						haveXDefault = true;
					}
					break;
				}

				default:
				{
					// does not happen under normal circumstances
					throw new XMPException("Unexpected result from ChooseLocalizedText", XMPError.INTERNALFAILURE
						);
				}
			}
			// Add an x-default at the front if needed.
			if (!haveXDefault && arrayNode.GetChildrenLength() == 1)
			{
				XMPNodeUtils.AppendLangItem(arrayNode, X_DEFAULT, itemValue);
			}
		}

		public virtual void SetLocalizedText(string schemaNS, string altTextName, string 
			genericLang, string specificLang, string itemValue)
		{
			SetLocalizedText(schemaNS, altTextName, genericLang, specificLang, itemValue, null
				);
		}

		public virtual XMPProperty GetProperty(string schemaNS, string propName)
		{
			return GetProperty(schemaNS, propName, VALUE_STRING);
		}

		/// <summary>Returns a property, but the result value can be requested.</summary>
		/// <remarks>
		/// Returns a property, but the result value can be requested. It can be one
		/// of
		/// <see cref="VALUE_STRING"/>
		/// ,
		/// <see cref="VALUE_BOOLEAN"/>
		/// ,
		/// <see cref="VALUE_INTEGER"/>
		/// ,
		/// <see cref="VALUE_LONG"/>
		/// ,
		/// <see cref="VALUE_DOUBLE"/>
		/// ,
		/// <see cref="VALUE_DATE"/>
		/// ,
		/// <see cref="VALUE_CALENDAR"/>
		/// ,
		/// <see cref="VALUE_BASE64"/>
		/// .
		/// </remarks>
		/// <seealso cref="XMPMeta.GetProperty(System.String, System.String)
		/// 	"/>
		/// <param name="schemaNS">a schema namespace</param>
		/// <param name="propName">a property name or path</param>
		/// <param name="valueType">the type of the value, see VALUE_...</param>
		/// <returns>Returns an <code>XMPProperty</code></returns>
		/// <exception cref="XMPException">Collects any exception that occurs.</exception>
		protected internal virtual XMPProperty GetProperty(string schemaNS, string propName
			, int valueType)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertPropName(propName);
			var expPath = XMPPathParser.ExpandXPath(schemaNS, propName);
			var propNode = XMPNodeUtils.FindNode(tree, expPath, false, null);
			if (propNode != null)
			{
				if (valueType != VALUE_STRING && propNode.GetOptions().IsCompositeProperty())
				{
					throw new XMPException("Property must be simple when a value type is requested", 
						XMPError.BADXPATH);
				}
				var value = EvaluateNodeValue(valueType, propNode);
				return new _XMPProperty_703(value, propNode);
			}

			return null;
		}

		private sealed class _XMPProperty_703 : XMPProperty
		{
			public _XMPProperty_703(object value, XMPNode propNode)
			{
				this.value = value;
				this.propNode = propNode;
			}

			public string GetValue()
			{
				return value != null ? value.ToString() : null;
			}

			public PropertyOptions GetOptions()
			{
				return propNode.GetOptions();
			}

			public string GetLanguage()
			{
				return null;
			}

			public override string ToString()
			{
				return value.ToString();
			}

			private readonly object value;

			private readonly XMPNode propNode;
		}

		/// <summary>Returns a property, but the result value can be requested.</summary>
		/// <seealso cref="XMPMeta.GetProperty(System.String, System.String)
		/// 	"/>
		/// <param name="schemaNS">a schema namespace</param>
		/// <param name="propName">a property name or path</param>
		/// <param name="valueType">the type of the value, see VALUE_...</param>
		/// <returns>
		/// Returns the node value as an object according to the
		/// <code>valueType</code>.
		/// </returns>
		/// <exception cref="XMPException">Collects any exception that occurs.</exception>
		protected internal virtual object GetPropertyObject(string schemaNS, string propName
			, int valueType)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertPropName(propName);
			var expPath = XMPPathParser.ExpandXPath(schemaNS, propName);
			var propNode = XMPNodeUtils.FindNode(tree, expPath, false, null);
			if (propNode != null)
			{
				if (valueType != VALUE_STRING && propNode.GetOptions().IsCompositeProperty())
				{
					throw new XMPException("Property must be simple when a value type is requested", 
						XMPError.BADXPATH);
				}
				return EvaluateNodeValue(valueType, propNode);
			}

			return null;
		}

		public virtual bool? GetPropertyBoolean(string schemaNS, string propName)
		{
			return (bool?)GetPropertyObject(schemaNS, propName, VALUE_BOOLEAN);
		}

		public virtual void SetPropertyBoolean(string schemaNS, string propName, bool propValue
			, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue ? TRUESTR : FALSESTR, options);
		}

		public virtual void SetPropertyBoolean(string schemaNS, string propName, bool propValue
			)
		{
			SetProperty(schemaNS, propName, propValue ? TRUESTR : FALSESTR, null);
		}

		public virtual int? GetPropertyInteger(string schemaNS, string propName)
		{
			return (int?)GetPropertyObject(schemaNS, propName, VALUE_INTEGER);
		}

		public virtual void SetPropertyInteger(string schemaNS, string propName, int propValue
			, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyInteger(string schemaNS, string propName, int propValue
			)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual long? GetPropertyLong(string schemaNS, string propName)
		{
			return (long?) GetPropertyObject(schemaNS, propName, VALUE_LONG);
		}

		public virtual void SetPropertyLong(string schemaNS, string propName, long propValue
			, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyLong(string schemaNS, string propName, long propValue
			)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual double? GetPropertyDouble(string schemaNS, string propName)
		{
			return (double?)GetPropertyObject(schemaNS, propName, VALUE_DOUBLE);
		}

		public virtual void SetPropertyDouble(string schemaNS, string propName, double propValue
			, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyDouble(string schemaNS, string propName, double propValue
			)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual XMPDateTime GetPropertyDate(string schemaNS, string propName)
		{
			return (XMPDateTime)GetPropertyObject(schemaNS, propName, VALUE_DATE);
		}

		public virtual void SetPropertyDate(string schemaNS, string propName, XMPDateTime
			 propValue, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyDate(string schemaNS, string propName, XMPDateTime
			 propValue)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual DateTime GetPropertyCalendar(string schemaNS, string propName)
		{
			return (DateTime)GetPropertyObject(schemaNS, propName, VALUE_CALENDAR);
		}

		public virtual void SetPropertyCalendar(string schemaNS, string propName, DateTime
			 propValue, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyCalendar(string schemaNS, string propName, DateTime
			 propValue)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual byte[] GetPropertyBase64(string schemaNS, string propName)
		{
			return (byte[])GetPropertyObject(schemaNS, propName, VALUE_BASE64);
		}

		public virtual string GetPropertyString(string schemaNS, string propName)
		{
			return (string)GetPropertyObject(schemaNS, propName, VALUE_STRING);
		}

		public virtual void SetPropertyBase64(string schemaNS, string propName, byte[] propValue
			, PropertyOptions options)
		{
			SetProperty(schemaNS, propName, propValue, options);
		}

		public virtual void SetPropertyBase64(string schemaNS, string propName, byte[] propValue
			)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual XMPProperty GetQualifier(string schemaNS, string propName, string 
			qualNS, string qualName)
		{
			// qualNS and qualName are checked inside composeQualfierPath
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertPropName(propName);
			var qualPath = propName + XMPPathFactory.ComposeQualifierPath(qualNS, qualName
				);
			return GetProperty(schemaNS, qualPath);
		}

		public virtual XMPProperty GetStructField(string schemaNS, string structName, string
			 fieldNS, string fieldName)
		{
			// fieldNS and fieldName are checked inside composeStructFieldPath
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertStructName(structName);
			var fieldPath = structName + XMPPathFactory.ComposeStructFieldPath(fieldNS, fieldName
				);
			return GetProperty(schemaNS, fieldPath);
		}

		public virtual XMPIterator Iterator()
		{
			return Iterator(null, null, null);
		}

		public virtual XMPIterator Iterator(IteratorOptions options)
		{
			return Iterator(null, null, options);
		}

		public virtual XMPIterator Iterator(string schemaNS, string propName, IteratorOptions
			 options)
		{
			return new XMPIteratorImpl(this, schemaNS, propName, options);
		}

		public virtual void SetArrayItem(string schemaNS, string arrayName, int itemIndex
			, string itemValue, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(arrayName);
			// Just lookup, don't try to create.
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, arrayName);
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, false, null);
			if (arrayNode != null)
			{
				DoSetArrayItem(arrayNode, itemIndex, itemValue, options, false);
			}
			else
			{
				throw new XMPException("Specified array does not exist", XMPError.BADXPATH);
			}
		}

		public virtual void SetArrayItem(string schemaNS, string arrayName, int itemIndex
			, string itemValue)
		{
			SetArrayItem(schemaNS, arrayName, itemIndex, itemValue, null);
		}

		public virtual void InsertArrayItem(string schemaNS, string arrayName, int itemIndex
			, string itemValue, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertArrayName(arrayName);
			// Just lookup, don't try to create.
			var arrayPath = XMPPathParser.ExpandXPath(schemaNS, arrayName);
			var arrayNode = XMPNodeUtils.FindNode(tree, arrayPath, false, null);
			if (arrayNode != null)
			{
				DoSetArrayItem(arrayNode, itemIndex, itemValue, options, true);
			}
			else
			{
				throw new XMPException("Specified array does not exist", XMPError.BADXPATH);
			}
		}

		public virtual void InsertArrayItem(string schemaNS, string arrayName, int itemIndex
			, string itemValue)
		{
			InsertArrayItem(schemaNS, arrayName, itemIndex, itemValue, null);
		}

		public virtual void SetProperty(string schemaNS, string propName, object propValue
			, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertPropName(propName);
			options = XMPNodeUtils.VerifySetOptions(options, propValue);
			var expPath = XMPPathParser.ExpandXPath(schemaNS, propName);
			var propNode = XMPNodeUtils.FindNode(tree, expPath, true, options);
			if (propNode != null)
			{
				SetNode(propNode, propValue, options, false);
			}
			else
			{
				throw new XMPException("Specified property does not exist", XMPError.BADXPATH);
			}
		}

		public virtual void SetProperty(string schemaNS, string propName, object propValue
			)
		{
			SetProperty(schemaNS, propName, propValue, null);
		}

		public virtual void SetQualifier(string schemaNS, string propName, string qualNS, 
			string qualName, string qualValue, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertPropName(propName);
			if (!DoesPropertyExist(schemaNS, propName))
			{
				throw new XMPException("Specified property does not exist!", XMPError.BADXPATH);
			}
			var qualPath = propName + XMPPathFactory.ComposeQualifierPath(qualNS, qualName
				);
			SetProperty(schemaNS, qualPath, qualValue, options);
		}

		public virtual void SetQualifier(string schemaNS, string propName, string qualNS, 
			string qualName, string qualValue)
		{
			SetQualifier(schemaNS, propName, qualNS, qualName, qualValue, null);
		}

		public virtual void SetStructField(string schemaNS, string structName, string fieldNS
			, string fieldName, string fieldValue, PropertyOptions options)
		{
			ParameterAsserts.AssertSchemaNS(schemaNS);
			ParameterAsserts.AssertStructName(structName);
			var fieldPath = structName + XMPPathFactory.ComposeStructFieldPath(fieldNS, fieldName
				);
			SetProperty(schemaNS, fieldPath, fieldValue, options);
		}

		public virtual void SetStructField(string schemaNS, string structName, string fieldNS
			, string fieldName, string fieldValue)
		{
			SetStructField(schemaNS, structName, fieldNS, fieldName, fieldValue, null);
		}

		public virtual string GetObjectName()
		{
			return tree.GetName() != null ? tree.GetName() : "";
		}

		public virtual void SetObjectName(string name)
		{
			tree.SetName(name);
		}

		public virtual string GetPacketHeader()
		{
			return packetHeader;
		}

		/// <summary>Sets the packetHeader attributes, only used by the parser.</summary>
		/// <param name="packetHeader">the processing instruction content</param>
		public virtual void SetPacketHeader(string packetHeader)
		{
			this.packetHeader = packetHeader;
		}

		/// <summary>Performs a deep clone of the XMPMeta-object</summary>
		/// <seealso cref="System.Object.Clone()"/>
		public virtual object Clone()
		{
			var clonedTree = (XMPNode)tree.Clone();
			return new XMPMetaImpl(clonedTree);
		}

		public virtual string DumpObject()
		{
			// renders tree recursively
			return GetRoot().DumpNode(true);
		}

		public virtual void Sort()
		{
			tree.Sort();
		}

		public virtual void Normalize(ParseOptions options)
		{
			if (options == null)
			{
				options = new ParseOptions();
			}
			XMPNormalizer.Process(this, options);
		}

		/// <returns>Returns the root node of the XMP tree.</returns>
		public virtual XMPNode GetRoot()
		{
			return tree;
		}

		// -------------------------------------------------------------------------------------
		// private
		/// <summary>Locate or create the item node and set the value.</summary>
		/// <remarks>
		/// Locate or create the item node and set the value. Note the index
		/// parameter is one-based! The index can be in the range [1..size + 1] or
		/// "last()", normalize it and check the insert flags. The order of the
		/// normalization checks is important. If the array is empty we end up with
		/// an index and location to set item size + 1.
		/// </remarks>
		/// <param name="arrayNode">an array node</param>
		/// <param name="itemIndex">the index where to insert the item</param>
		/// <param name="itemValue">the item value</param>
		/// <param name="itemOptions">the options for the new item</param>
		/// <param name="insert">insert oder overwrite at index position?</param>
		/// <exception cref="XMPException">array item cannot be set</exception>
		private void DoSetArrayItem(XMPNode arrayNode, int itemIndex, string itemValue, PropertyOptions
			 itemOptions, bool insert)
		{
			var itemNode = new XMPNode(ARRAY_ITEM_NAME, null);
			itemOptions = XMPNodeUtils.VerifySetOptions(itemOptions, itemValue);
			// in insert mode the index after the last is allowed,
			// even ARRAY_LAST_ITEM points to the index *after* the last.
			var maxIndex = insert ? arrayNode.GetChildrenLength() + 1 : arrayNode.GetChildrenLength
				();
			if (itemIndex == ARRAY_LAST_ITEM)
			{
				itemIndex = maxIndex;
			}
			if (1 <= itemIndex && itemIndex <= maxIndex)
			{
				if (!insert)
				{
					arrayNode.RemoveChild(itemIndex);
				}
				arrayNode.AddChild(itemIndex, itemNode);
				SetNode(itemNode, itemValue, itemOptions, false);
			}
			else
			{
				throw new XMPException("Array index out of bounds", XMPError.BADINDEX);
			}
		}

		/// <summary>
		/// The internals for setProperty() and related calls, used after the node is
		/// found or created.
		/// </summary>
		/// <param name="node">the newly created node</param>
		/// <param name="value">the node value, can be <code>null</code></param>
		/// <param name="newOptions">options for the new node, must not be <code>null</code>.
		/// 	</param>
		/// <param name="deleteExisting">flag if the existing value is to be overwritten</param>
		/// <exception cref="XMPException">thrown if options and value do not correspond</exception>
		internal virtual void SetNode(XMPNode node, object value, PropertyOptions newOptions
			, bool deleteExisting)
		{
			if (deleteExisting)
			{
				node.Clear();
			}
			// its checked by setOptions(), if the merged result is a valid options set
			node.GetOptions().MergeWith(newOptions);
			if (!node.GetOptions().IsCompositeProperty())
			{
				// This is setting the value of a leaf node.
				XMPNodeUtils.SetNodeValue(node, value);
			}
			else
			{
				if (value != null && value.ToString().Length > 0)
				{
					throw new XMPException("Composite nodes can't have values", XMPError.BADXPATH);
				}
				node.RemoveChildren();
			}
		}

		/// <summary>
		/// Evaluates a raw node value to the given value type, apply special
		/// conversions for defined types in XMP.
		/// </summary>
		/// <param name="valueType">an int indicating the value type</param>
		/// <param name="propNode">the node containing the value</param>
		/// <returns>Returns a literal value for the node.</returns>
		/// <exception cref="XMPException">
		/// if the value of <code>propNode</code> is <code>null</code> or empty or the conversion fails.
		/// </exception>
		private object EvaluateNodeValue(int valueType, XMPNode propNode)
		{
			object value;
			var rawValue = propNode.GetValue();
			switch (valueType)
			{
				case VALUE_BOOLEAN:
				{
					value = XMPUtils.ConvertToBoolean(rawValue);
					break;
				}

				case VALUE_INTEGER:
				{
					value = XMPUtils.ConvertToInteger(rawValue);
					break;
				}

				case VALUE_LONG:
				{
					value = XMPUtils.ConvertToLong(rawValue);
					break;
				}

				case VALUE_DOUBLE:
				{
					value = XMPUtils.ConvertToDouble(rawValue);
					break;
				}

				case VALUE_DATE:
				{
					value = XMPUtils.ConvertToDate(rawValue);
					break;
				}

				case VALUE_CALENDAR:
				{
					var dt = XMPUtils.ConvertToDate(rawValue);
					value = dt.GetCalendar();
					break;
				}

				case VALUE_BASE64:
				{
					value = XMPUtils.DecodeBase64(rawValue);
					break;
				}

				case VALUE_STRING:
				default:
				{
					// leaf values return empty string instead of null
					// for the other cases the converter methods provides a "null"
					// value.
					// a default value can only occur if this method is made public.
					value = rawValue != null || propNode.GetOptions().IsCompositeProperty() ? rawValue
						 : "";
					break;
				}
			}
			return value;
		}
	}
}
