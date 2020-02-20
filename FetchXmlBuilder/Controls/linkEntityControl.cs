﻿using Cinteros.Xrm.FetchXmlBuilder.AppCode;
using Cinteros.Xrm.FetchXmlBuilder.DockControls;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Cinteros.Xrm.FetchXmlBuilder.Controls
{
    public partial class linkEntityControl : FetchXmlElementControlBase
    {
        private int relationshipWidth;

        public linkEntityControl() : this(null, null, null)
        {
        }

        public linkEntityControl(TreeNode node, FetchXmlBuilder fetchXmlBuilder, TreeBuilderControl tree)
        {
            InitializeComponent();
            InitializeFXB(null, fetchXmlBuilder, tree, node);
            warningProvider.Icon = WarningIcon;
        }

        protected override void PopulateControls()
        {
            cmbEntity.Items.Clear();
            var entities = fxb.GetDisplayEntities();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    cmbEntity.Items.Add(entity.Value.LogicalName);
                }
            }

            var parententityname = TreeNodeHelper.GetAttributeFromNode(Node.Parent, "name");
            if (fxb.NeedToLoadEntity(parententityname))
            {
                if (!fxb.working)
                {
                    fxb.LoadEntityDetails(parententityname, RefreshRelationships);
                }
            }
            else
            {
                RefreshRelationships();
            }
            RefreshAttributes();
        }

        private void cmbEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            var entity = cmbEntity.SelectedItem.ToString();
            if (string.IsNullOrEmpty(entity))
            {
                return;
            }
            if (fxb.NeedToLoadEntity(entity))
            {
                if (!fxb.working)
                {
                    fxb.LoadEntityDetails(entity, RefreshAttributes);
                }
            }
            else
            {
                RefreshAttributes();
            }
        }

        private void RefreshRelationships()
        {
            cmbRelationship.Items.Clear();
            var parententityname = TreeNodeHelper.GetAttributeFromNode(Node.Parent, "name");
            var entities = fxb.GetDisplayEntities();
            if (entities != null && entities.ContainsKey(parententityname))
            {
                var parententity = entities[parententityname];
                var mo = parententity.ManyToOneRelationships;
                var om = parententity.OneToManyRelationships;
                var mm = parententity.ManyToManyRelationships;
                var list = new List<EntityRelationship>();
                if (mo.Length > 0)
                {
                    cmbRelationship.Items.Add("- M:1 -");
                    list.Clear();
                    foreach (var rel in mo)
                    {
                        list.Add(new EntityRelationship(rel, parententityname, fxb));
                    }
                    list.Sort();
                    cmbRelationship.Items.AddRange(list.ToArray());
                }
                if (om.Length > 0)
                {
                    cmbRelationship.Items.Add("- 1:M -");
                    list.Clear();
                    foreach (var rel in om)
                    {
                        list.Add(new EntityRelationship(rel, parententityname, fxb));
                    }
                    list.Sort();
                    cmbRelationship.Items.AddRange(list.ToArray());
                }
                if (mm.Length > 0)
                {
                    var greatparententityname = Node.Parent.Parent != null ? TreeNodeHelper.GetAttributeFromNode(Node.Parent.Parent, "name") : "";
                    cmbRelationship.Items.Add("- M:M -");
                    list.Clear();
                    foreach (var rel in mm)
                    {
                        list.Add(new EntityRelationship(rel, parententityname, fxb, greatparententityname));
                    }
                    list.Sort();
                    cmbRelationship.Items.AddRange(list.ToArray());
                }
            }
        }

        private void RefreshAttributes()
        {
            cmbFrom.Items.Clear();
            cmbTo.Items.Clear();
            if (cmbEntity.SelectedItem != null)
            {
                var linkentity = cmbEntity.SelectedItem.ToString();
                var linkAttributes = fxb.GetDisplayAttributes(linkentity);
                foreach (var attribute in linkAttributes)
                {
                    if (attribute.IsPrimaryId == true ||
                        attribute.AttributeType == AttributeTypeCode.Lookup ||
                        attribute.AttributeType == AttributeTypeCode.Customer ||
                        attribute.AttributeType == AttributeTypeCode.Owner)
                    {
                        cmbFrom.Items.Add(attribute.LogicalName);
                    }
                }
            }
            var parententity = TreeNodeHelper.GetAttributeFromNode(Node.Parent, "name");
            var parentAttributes = fxb.GetDisplayAttributes(parententity);
            foreach (var attribute in parentAttributes)
            {
                if (attribute.IsPrimaryId == true ||
                    attribute.AttributeType == AttributeTypeCode.Lookup ||
                    attribute.AttributeType == AttributeTypeCode.Customer ||
                    attribute.AttributeType == AttributeTypeCode.Owner)
                {
                    cmbTo.Items.Add(attribute.LogicalName);
                }
            }
        }

        private void cmbRelationship_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbRelationship.SelectedItem != null && cmbRelationship.SelectedItem is EntityRelationship)
            {
                var rel = (EntityRelationship)cmbRelationship.SelectedItem;
                var parent = TreeNodeHelper.GetAttributeFromNode(Node.Parent, "name");
                if (rel.Relationship is OneToManyRelationshipMetadata)
                {
                    var om = (OneToManyRelationshipMetadata)rel.Relationship;
                    if (parent == om.ReferencedEntity)
                    {
                        cmbEntity.Text = om.ReferencingEntity;
                        cmbFrom.Text = om.ReferencingAttribute;
                        cmbTo.Text = om.ReferencedAttribute;
                    }
                    else if (parent == om.ReferencingEntity)
                    {
                        cmbEntity.Text = om.ReferencedEntity;
                        cmbFrom.Text = om.ReferencedAttribute;
                        cmbTo.Text = om.ReferencingAttribute;
                    }
                    else
                    {
                        MessageBox.Show("Not a valid relationship. Please enter entity and attributes manually.");
                    }
                    chkIntersect.Checked = false;
                }
                else if (rel.Relationship is ManyToManyRelationshipMetadata)
                {
                    var mm = (ManyToManyRelationshipMetadata)rel.Relationship;
                    if (parent == mm.IntersectEntityName)
                    {
                        var greatparent = TreeNodeHelper.GetAttributeFromNode(Node.Parent.Parent, "name");
                        if (greatparent == mm.Entity1LogicalName)
                        {
                            cmbEntity.Text = mm.Entity2LogicalName;
                            cmbFrom.Text = mm.Entity2IntersectAttribute;
                            cmbTo.Text = mm.Entity2IntersectAttribute;
                        }
                        else if (greatparent == mm.Entity2LogicalName)
                        {
                            cmbEntity.Text = mm.Entity1LogicalName;
                            cmbFrom.Text = mm.Entity1IntersectAttribute;
                            cmbTo.Text = mm.Entity1IntersectAttribute;
                        }
                        else
                        {
                            MessageBox.Show("Not a valid M:M-relationship. Please enter entity and attributes manually.");
                        }
                    }
                    else
                    {
                        cmbEntity.Text = mm.IntersectEntityName;
                        if (parent == mm.Entity1LogicalName)
                        {
                            cmbFrom.Text = mm.Entity1IntersectAttribute;
                            cmbTo.Text = mm.Entity1IntersectAttribute;
                        }
                        else if (parent == mm.Entity2LogicalName)
                        {
                            cmbFrom.Text = mm.Entity2IntersectAttribute;
                            cmbTo.Text = mm.Entity2IntersectAttribute;
                        }
                        else
                        {
                            MessageBox.Show("Not a valid M:M-relationship. Please enter entity and attributes manually.");
                        }
                        chkIntersect.Checked = true;
                    }
                }
            }
        }

        private void cmbRelationship_DropDown(object sender, EventArgs e)
        {
            relationshipWidth = cmbRelationship.Width;
            if (cmbRelationship.Width < 300)
            {
                cmbRelationship.Width = 350;
            }
        }

        private void cmbRelationship_DropDownClosed(object sender, EventArgs e)
        {
            if (relationshipWidth < 300)
            {
                cmbRelationship.Width = relationshipWidth;
            }
        }

        protected override bool ValidateControls(bool silent)
        {
            var valid = base.ValidateControls(silent);

            if (string.IsNullOrWhiteSpace(cmbEntity.Text))
            {
                if (!silent)
                {
                    errorProvider.SetError(cmbEntity, "Entity is required");
                }
                
                valid = false;
            }


            if (string.IsNullOrWhiteSpace(cmbFrom.Text))
            {
                if (!silent)
                {
                    errorProvider.SetError(cmbFrom, "From attribute is required");
                }

                valid = false;
            }

            if (string.IsNullOrWhiteSpace(cmbTo.Text))
            {
                if (!silent)
                {
                    errorProvider.SetError(cmbTo, "To attribute is required");
                }

                valid = false;
            }

            return valid;
        }

        private void cmbEntity_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            errorProvider.SetError(cmbEntity, null);
            warningProvider.SetError(cmbEntity, null);

            if (string.IsNullOrWhiteSpace(cmbEntity.Text))
            {
                errorProvider.SetError(cmbEntity, "Entity is required");
            }
            else if (cmbEntity.SelectedIndex == -1)
            {
                warningProvider.SetError(cmbEntity, "Entity is not valid");
            }
        }

        private void cmbFrom_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            errorProvider.SetError(cmbFrom, null);
            warningProvider.SetError(cmbFrom, null);
            
            if (string.IsNullOrWhiteSpace(cmbFrom.Text))
            {
                errorProvider.SetError(cmbFrom, "From attribute is required");
            }
            else if (cmbFrom.SelectedIndex == -1)
            {
                warningProvider.SetError(cmbFrom, "From attribute is not valid");
            }
        }

        private void cmbTo_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            errorProvider.SetError(cmbTo, null);
            warningProvider.SetError(cmbTo, null);

            if (string.IsNullOrWhiteSpace(cmbTo.Text))
            {
                errorProvider.SetError(cmbTo, "To attribute is required");
            }
            else if (cmbTo.SelectedIndex == -1)
            {
                warningProvider.SetError(cmbTo, "To attribute is not valid");
            }
        }
    }
}