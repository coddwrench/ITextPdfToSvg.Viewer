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
using System.Collections;
using System.Diagnostics;
using System.Text;
using IText.IO.Util;
using IText.Kernel.XMP.Options;

namespace IText.Kernel.XMP.Impl
{
	/// <summary>
	/// A node in the internally XMP tree, which can be a schema node, a property node, an array node,
	/// an array item, a struct node or a qualifier node (without '?').
	/// </summary>
	/// <remarks>
	/// A node in the internally XMP tree, which can be a schema node, a property node, an array node,
	/// an array item, a struct node or a qualifier node (without '?').
	/// Possible improvements:
	/// 1. The kind Node of node might be better represented by a class-hierarchy of different nodes.
	/// 2. The array type should be an enum
	/// 3. isImplicitNode should be removed completely and replaced by return values of fi.
	/// 4. hasLanguage, hasType should be automatically maintained by XMPNode
	/// </remarks>
	/// <since>21.02.2006</since>
	public class XMPNode : IComparable
	{
		/// <summary>name of the node, contains different information depending of the node kind
		/// 	</summary>
		private string name;

		/// <summary>value of the node, contains different information depending of the node kind
		/// 	</summary>
		private string value;

		/// <summary>link to the parent node</summary>
		private XMPNode parent;

		/// <summary>list of child nodes, lazy initialized</summary>
		private IList children;

		/// <summary>list of qualifier of the node, lazy initialized</summary>
		private IList qualifier;

		/// <summary>options describing the kind of the node</summary>
		private PropertyOptions options;

		/// <summary>flag if the node is implicitly created</summary>
		private bool @implicit;

		/// <summary>flag if the node has aliases</summary>
		private bool hasAliases;

		/// <summary>flag if the node is an alias</summary>
		private bool alias;

		/// <summary>flag if the node has an "rdf:value" child node.</summary>
		private bool hasValueChild;

		/// <summary>Creates an <code>XMPNode</code> with initial values.</summary>
		/// <param name="name">the name of the node</param>
		/// <param name="value">the value of the node</param>
		/// <param name="options">the options of the node</param>
		public XMPNode(string name, string value, PropertyOptions options)
		{
			// internal processing options
			this.name = name;
			this.value = value;
			this.options = options;
		}

		/// <summary>Constructor for the node without value.</summary>
		/// <param name="name">the name of the node</param>
		/// <param name="options">the options of the node</param>
		public XMPNode(string name, PropertyOptions options)
			: this(name, null, options)
		{
		}

		/// <summary>Resets the node.</summary>
		public virtual void Clear()
		{
			options = null;
			name = null;
			value = null;
			children = null;
			qualifier = null;
		}

		/// <returns>Returns the parent node.</returns>
		public virtual XMPNode GetParent()
		{
			return parent;
		}

		/// <param name="index">an index [1..size]</param>
		/// <returns>Returns the child with the requested index.</returns>
		public virtual XMPNode GetChild(int index)
		{
			return (XMPNode)GetChildren()[index - 1];
		}

		/// <summary>Adds a node as child to this node.</summary>
		/// <param name="node">an XMPNode</param>
		public virtual void AddChild(XMPNode node)
		{
			// check for duplicate properties
			AssertChildNotExisting(node.GetName());
			node.SetParent(this);
			GetChildren().Add(node);
		}

		/// <summary>Adds a node as child to this node.</summary>
		/// <param name="index">
		/// the index of the node <em>before</em> which the new one is inserted.
		/// <em>Note:</em> The node children are indexed from [1..size]!
		/// An index of size + 1 appends a node.
		/// </param>
		/// <param name="node">an XMPNode</param>
		public virtual void AddChild(int index, XMPNode node)
		{
			AssertChildNotExisting(node.GetName());
			node.SetParent(this);
			GetChildren().Insert(index - 1, node);
		}

		/// <summary>Replaces a node with another one.</summary>
		/// <param name="index">
		/// the index of the node that will be replaced.
		/// <em>Note:</em> The node children are indexed from [1..size]!
		/// </param>
		/// <param name="node">the replacement XMPNode</param>
		public virtual void ReplaceChild(int index, XMPNode node
			)
		{
			node.SetParent(this);
			GetChildren()[index - 1] = node;
		}

		/// <summary>Removes a child at the requested index.</summary>
		/// <param name="itemIndex">the index to remove [1..size]</param>
		public virtual void RemoveChild(int itemIndex)
		{
			GetChildren().RemoveAt(itemIndex - 1);
			CleanupChildren();
		}

		/// <summary>Removes a child node.</summary>
		/// <remarks>
		/// Removes a child node.
		/// If its a schema node and doesn't have any children anymore, its deleted.
		/// </remarks>
		/// <param name="node">the child node to delete.</param>
		public virtual void RemoveChild(XMPNode node)
		{
			GetChildren().Remove(node);
			CleanupChildren();
		}

		/// <summary>
		/// Removes the children list if this node has no children anymore;
		/// checks if the provided node is a schema node and doesn't have any children anymore,
		/// its deleted.
		/// </summary>
		protected internal virtual void CleanupChildren()
		{
			if (children.Count == 0)
			{
				children = null;
			}
		}

		/// <summary>Removes all children from the node.</summary>
		public virtual void RemoveChildren()
		{
			children = null;
		}

		/// <returns>Returns the number of children without neccessarily creating a list.</returns>
		public virtual int GetChildrenLength()
		{
			return children != null ? children.Count : 0;
		}

		/// <param name="expr">child node name to look for</param>
		/// <returns>Returns an <code>XMPNode</code> if node has been found, <code>null</code> otherwise.
		/// 	</returns>
		public virtual XMPNode FindChildByName(string expr)
		{
			return Find(GetChildren(), expr);
		}

		/// <param name="index">an index [1..size]</param>
		/// <returns>Returns the qualifier with the requested index.</returns>
		public virtual XMPNode GetQualifier(int index)
		{
			return (XMPNode)GetQualifier()[index - 1];
		}

		/// <returns>Returns the number of qualifier without neccessarily creating a list.</returns>
		public virtual int GetQualifierLength()
		{
			return qualifier != null ? qualifier.Count : 0;
		}

		/// <summary>Appends a qualifier to the qualifier list and sets respective options.</summary>
		/// <param name="qualNode">a qualifier node.</param>
		public virtual void AddQualifier(XMPNode qualNode)
		{
			AssertQualifierNotExisting(qualNode.GetName());
			qualNode.SetParent(this);
			qualNode.GetOptions().SetQualifier(true);
			GetOptions().SetHasQualifiers(true);
			// contraints
			if (qualNode.IsLanguageNode())
			{
				// "xml:lang" is always first and the option "hasLanguage" is set
				options.SetHasLanguage(true);
				GetQualifier().Insert(0, qualNode);
			}
			else
			{
				if (qualNode.IsTypeNode())
				{
					// "rdf:type" must be first or second after "xml:lang" and the option "hasType" is set
					options.SetHasType(true);
					GetQualifier().Insert(!options.GetHasLanguage() ? 0 : 1, qualNode);
				}
				else
				{
					// other qualifiers are appended
					GetQualifier().Add(qualNode);
				}
			}
		}

		/// <summary>Removes one qualifier node and fixes the options.</summary>
		/// <param name="qualNode">qualifier to remove</param>
		public virtual void RemoveQualifier(XMPNode qualNode)
		{
			var opts = GetOptions();
			if (qualNode.IsLanguageNode())
			{
				// if "xml:lang" is removed, remove hasLanguage-flag too
				opts.SetHasLanguage(false);
			}
			else
			{
				if (qualNode.IsTypeNode())
				{
					// if "rdf:type" is removed, remove hasType-flag too
					opts.SetHasType(false);
				}
			}
			GetQualifier().Remove(qualNode);
			if (qualifier.Count == 0)
			{
				opts.SetHasQualifiers(false);
				qualifier = null;
			}
		}

		/// <summary>Removes all qualifiers from the node and sets the options appropriate.</summary>
		public virtual void RemoveQualifiers()
		{
			var opts = GetOptions();
			// clear qualifier related options
			opts.SetHasQualifiers(false);
			opts.SetHasLanguage(false);
			opts.SetHasType(false);
			qualifier = null;
		}

		/// <param name="expr">qualifier node name to look for</param>
		/// <returns>
		/// Returns a qualifier <code>XMPNode</code> if node has been found,
		/// <code>null</code> otherwise.
		/// </returns>
		public virtual XMPNode FindQualifierByName(string expr
			)
		{
			return Find(qualifier, expr);
		}

		/// <returns>Returns whether the node has children.</returns>
		public virtual bool HasChildren()
		{
			return children != null && children.Count > 0;
		}

		/// <returns>
		/// Returns an iterator for the children.
		/// <em>Note:</em> take care to use it.remove(), as the flag are not adjusted in that case.
		/// </returns>
		public virtual IEnumerator IterateChildren()
		{
			if (children != null)
			{
				return GetChildren().GetEnumerator();
			}

			return JavaCollectionsUtil.EmptyIterator();
		}

		/// <returns>Returns whether the node has qualifier attached.</returns>
		public virtual bool HasQualifier()
		{
			return qualifier != null && qualifier.Count > 0;
		}

		/// <returns>
		/// Returns an iterator for the qualifier.
		/// <em>Note:</em> take care to use it.remove(), as the flag are not adjusted in that case.
		/// </returns>
		public virtual IEnumerator IterateQualifier()
		{
			if (qualifier != null)
			{
				var it = GetQualifier().GetEnumerator();
				return it;
			}

			return JavaCollectionsUtil.EmptyIterator();
		}

		/// <summary>Performs a <b>deep clone</b> of the node and the complete subtree.</summary>
		/// <seealso cref="System.Object.Clone()"/>
		public virtual object Clone()
		{
			PropertyOptions newOptions;
			try
			{
				newOptions = new PropertyOptions(GetOptions().GetOptions());
			}
			catch (XMPException)
			{
				// cannot happen
				newOptions = new PropertyOptions();
			}
			var newNode = new XMPNode
				(name, value, newOptions);
			CloneSubtree(newNode);
			return newNode;
		}

		/// <summary>
		/// Performs a <b>deep clone</b> of the complete subtree (children and
		/// qualifier )into and add it to the destination node.
		/// </summary>
		/// <param name="destination">the node to add the cloned subtree</param>
		public virtual void CloneSubtree(XMPNode destination)
		{
			try
			{
				for (var it = IterateChildren(); it.MoveNext(); )
				{
					var child = (XMPNode)it
						.Current;
					destination.AddChild((XMPNode)child.Clone());
				}
				for (var it_1 = IterateQualifier(); it_1.MoveNext(); )
				{
					var qualifier = (XMPNode
						)it_1.Current;
					destination.AddQualifier((XMPNode)qualifier.Clone());
				}
			}
			catch (XMPException)
			{
				// cannot happen (duplicate childs/quals do not exist in this node)
				Debug.Assert(false);
			}
		}

		/// <summary>Renders this node and the tree unter this node in a human readable form.
		/// 	</summary>
		/// <param name="recursive">Flag is qualifier and child nodes shall be rendered too</param>
		/// <returns>Returns a multiline string containing the dump.</returns>
		public virtual string DumpNode(bool recursive)
		{
			var result = new StringBuilder(512);
			DumpNode(result, recursive, 0, 0);
			return result.ToString();
		}

		/// <seealso cref="System.IComparable{T}.CompareTo(System.Object)"></seealso>
		public virtual int CompareTo(object xmpNode)
		{
			if (GetOptions().IsSchemaNode())
			{
				return string.CompareOrdinal(value, ((XMPNode)xmpNode
					).GetValue());
			}

			return string.CompareOrdinal(name, ((XMPNode)xmpNode
				).GetName());
		}

		/// <returns>Returns the name.</returns>
		public virtual string GetName()
		{
			return name;
		}

		/// <param name="name">The name to set.</param>
		public virtual void SetName(string name)
		{
			this.name = name;
		}

		/// <returns>Returns the value.</returns>
		public virtual string GetValue()
		{
			return value;
		}

		/// <param name="value">The value to set.</param>
		public virtual void SetValue(string value)
		{
			this.value = value;
		}

		/// <returns>Returns the options.</returns>
		public virtual PropertyOptions GetOptions()
		{
			if (options == null)
			{
				options = new PropertyOptions();
			}
			return options;
		}

		/// <summary>Updates the options of the node.</summary>
		/// <param name="options">the options to set.</param>
		public virtual void SetOptions(PropertyOptions options)
		{
			this.options = options;
		}

		/// <returns>Returns the implicit flag</returns>
		public virtual bool IsImplicit()
		{
			return @implicit;
		}

		/// <param name="implicit">Sets the implicit node flag</param>
		public virtual void SetImplicit(bool @implicit)
		{
			this.@implicit = @implicit;
		}

		/// <returns>Returns if the node contains aliases (applies only to schema nodes)</returns>
		public virtual bool GetHasAliases()
		{
			return hasAliases;
		}

		/// <param name="hasAliases">sets the flag that the node contains aliases</param>
		public virtual void SetHasAliases(bool hasAliases)
		{
			this.hasAliases = hasAliases;
		}

		/// <returns>Returns if the node contains aliases (applies only to schema nodes)</returns>
		public virtual bool IsAlias()
		{
			return alias;
		}

		/// <param name="alias">sets the flag that the node is an alias</param>
		public virtual void SetAlias(bool alias)
		{
			this.alias = alias;
		}

		/// <returns>the hasValueChild</returns>
		public virtual bool GetHasValueChild()
		{
			return hasValueChild;
		}

		/// <param name="hasValueChild">the hasValueChild to set</param>
		public virtual void SetHasValueChild(bool hasValueChild)
		{
			this.hasValueChild = hasValueChild;
		}

		/// <summary>
		/// Sorts the complete datamodel according to the rules.
		/// </summary>
		/// <remarks>
		/// Sorts the complete datamodel according to the following rules:
		/// <ul>
		/// <li>Nodes at one level are sorted by name, that is prefix + local name</li>
		/// <li>Starting at the root node the children and qualifier are sorted recursively,
		/// which the following exceptions.</li>
		/// <li>Sorting will not be used for arrays.</li>
		/// <li>Within qualifier "xml:lang" and/or "rdf:type" stay at the top in that order,
		/// all others are sorted.</li>
		/// </ul>
		/// </remarks>
		public virtual void Sort()
		{
			// sort qualifier
			if (HasQualifier()) {
				var quals = new XMPNode[GetQualifier().Count];
				GetQualifier().CopyTo(quals, 0);
				var sortFrom = 0;
				while (quals.Length > sortFrom &&
					(XMPConst.XML_LANG.Equals(quals[sortFrom].GetName()) || "rdf:type".Equals(quals[sortFrom].GetName()))) {
					quals[sortFrom].Sort();
					sortFrom++;
				}
				Array.Sort(quals, sortFrom, quals.Length - sortFrom);
				for (var j = 0; j < quals.Length; j++) {
					qualifier[j] = quals[j];
					quals[j].Sort();
				}
			}

			// sort children
			if (HasChildren()) {
				if (!GetOptions().IsArray()) {
					ArrayList.Adapter(children).Sort();
				}
				var it = IterateChildren();
				while (it.MoveNext())
					if (it.Current != null)
						((XMPNode) it.Current).Sort();
			}
		}

		//------------------------------------------------------------------------------ private methods
		/// <summary>Dumps this node and its qualifier and children recursively.</summary>
		/// <remarks>
		/// Dumps this node and its qualifier and children recursively.
		/// <em>Note:</em> It creats empty options on every node.
		/// </remarks>
		/// <param name="result">the buffer to append the dump.</param>
		/// <param name="recursive">Flag is qualifier and child nodes shall be rendered too</param>
		/// <param name="indent">the current indent level.</param>
		/// <param name="index">the index within the parent node (important for arrays)</param>
		private void DumpNode(StringBuilder result, bool recursive, int indent, int index
			)
		{
			// write indent
			for (var i = 0; i < indent; i++)
			{
				result.Append('\t');
			}
			// render Node
			if (parent != null)
			{
				if (GetOptions().IsQualifier())
				{
					result.Append('?');
					result.Append(name);
				}
				else
				{
					if (GetParent().GetOptions().IsArray())
					{
						result.Append('[');
						result.Append(index);
						result.Append(']');
					}
					else
					{
						result.Append(name);
					}
				}
			}
			else
			{
				// applies only to the root node
				result.Append("ROOT NODE");
				if (name != null && name.Length > 0)
				{
					// the "about" attribute
					result.Append(" (");
					result.Append(name);
					result.Append(')');
				}
			}
			if (value != null && value.Length > 0)
			{
				result.Append(" = \"");
				result.Append(value);
				result.Append('"');
			}
			// render options if at least one is set
			if (GetOptions().ContainsOneOf(unchecked((int)(0xffffffff))))
			{
				result.Append("\t(");
				result.Append(GetOptions());
				result.Append(" : ");
				result.Append(GetOptions().GetOptionsString());
				result.Append(')');
			}
			result.Append('\n');
			// render qualifier
			if (recursive && HasQualifier())
			{
				var quals = new XMPNode[GetQualifier().Count];
				GetQualifier().CopyTo(quals, 0);
				var i = 0;
				while (quals.Length > i && (XMPConst.XML_LANG.Equals(quals[i].GetName()) || "rdf:type"
					.Equals(quals[i].GetName())))
				{
					i++;
				}
				Array.Sort(quals, i, quals.Length);
				for (i = 0; i < quals.Length; i++)
				{
					var qualifier = quals[i];
					qualifier.DumpNode(result, recursive, indent + 2, i + 1);
				}
			}
			// render children
			if (recursive && HasChildren())
			{
				var children = new XMPNode[GetChildren().Count];
				GetChildren().CopyTo(children, 0);
				if (!GetOptions().IsArray())
				{
					Array.Sort(children);
				}
				for (var i = 0; i < children.Length; i++)
				{
					var child = children[i];
					child.DumpNode(result, recursive, indent + 1, i + 1);
				}
			}
		}

		/// <returns>Returns whether this node is a language qualifier.</returns>
		private bool IsLanguageNode()
		{
			return XMPConst.XML_LANG.Equals(name);
		}

		/// <returns>Returns whether this node is a type qualifier.</returns>
		private bool IsTypeNode()
		{
			return "rdf:type".Equals(name);
		}

		/// <summary>
		/// <em>Note:</em> This method should always be called when accessing 'children' to be sure
		/// that its initialized.
		/// </summary>
		/// <returns>Returns list of children that is lazy initialized.</returns>
		protected internal virtual IList GetChildren()
		{
			if (children == null)
			{
				children = new ArrayList(0);
			}
			return children;
		}

		/// <returns>Returns a read-only copy of child nodes list.</returns>
		public virtual IList GetUnmodifiableChildren()
		{
			return JavaCollectionsUtil.UnmodifiableList(GetChildren());
		}

		/// <returns>Returns list of qualifier that is lazy initialized.</returns>
		private IList GetQualifier()
		{
			if (qualifier == null)
			{
				qualifier = new ArrayList(0);
			}
			return qualifier;
		}

		/// <summary>
		/// Sets the parent node, this is solely done by <code>addChild(...)</code>
		/// and <code>addQualifier()</code>.
		/// </summary>
		/// <param name="parent">Sets the parent node.</param>
		protected internal virtual void SetParent(XMPNode parent
			)
		{
			this.parent = parent;
		}

		/// <summary>Internal find.</summary>
		/// <param name="list">the list to search in</param>
		/// <param name="expr">the search expression</param>
		/// <returns>Returns the found node or <code>nulls</code>.</returns>
		private XMPNode Find(IList list, string expr)
		{
			if (list != null)
			{
				for (var it = list.GetEnumerator(); it.MoveNext(); )
				{
					var child = (XMPNode)it
						.Current;
					if (child.GetName().Equals(expr))
					{
						return child;
					}
				}
			}
			return null;
		}

		/// <summary>Checks that a node name is not existing on the same level, except for array items.
		/// 	</summary>
		/// <param name="childName">the node name to check</param>
		private void AssertChildNotExisting(string childName)
		{
			if (!XMPConst.ARRAY_ITEM_NAME.Equals(childName) && FindChildByName(childName) != 
				null)
			{
				throw new XMPException("Duplicate property or field node '" + childName + "'", XMPError
					.BADXMP);
			}
		}

		/// <summary>Checks that a qualifier name is not existing on the same level.</summary>
		/// <param name="qualifierName">the new qualifier name</param>
		private void AssertQualifierNotExisting(string qualifierName)
		{
			if (!XMPConst.ARRAY_ITEM_NAME.Equals(qualifierName) && FindQualifierByName(qualifierName
				) != null)
			{
				throw new XMPException("Duplicate '" + qualifierName + "' qualifier", XMPError.BADXMP
					);
			}
		}
	}
}
