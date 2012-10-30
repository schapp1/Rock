//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.Collections.Generic;
using System.Dynamic;

using Rock.Data;

namespace Rock.Cms
{
    /// <summary>
    /// Data Transfer Object for Auth object
    /// </summary>
    public partial class AuthDto : IDto
    {

#pragma warning disable 1591
        public int EntityTypeId { get; set; }
        public int? EntityId { get; set; }
        public int Order { get; set; }
        public string Action { get; set; }
        public string AllowOrDeny { get; set; }
        public SpecialRole SpecialRole { get; set; }
        public int? PersonId { get; set; }
        public int? GroupId { get; set; }
        public int Id { get; set; }
        public Guid Guid { get; set; }
#pragma warning restore 1591

        /// <summary>
        /// Instantiates a new DTO object
        /// </summary>
        public AuthDto ()
        {
        }

        /// <summary>
        /// Instantiates a new DTO object from the entity
        /// </summary>
        /// <param name="auth"></param>
        public AuthDto ( Auth auth )
        {
            CopyFromModel( auth );
        }

        /// <summary>
        /// Creates a dictionary object.
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>();
            dictionary.Add( "EntityTypeId", this.EntityTypeId );
            dictionary.Add( "EntityId", this.EntityId );
            dictionary.Add( "Order", this.Order );
            dictionary.Add( "Action", this.Action );
            dictionary.Add( "AllowOrDeny", this.AllowOrDeny );
            dictionary.Add( "SpecialRole", this.SpecialRole );
            dictionary.Add( "PersonId", this.PersonId );
            dictionary.Add( "GroupId", this.GroupId );
            dictionary.Add( "Id", this.Id );
            dictionary.Add( "Guid", this.Guid );
            return dictionary;
        }

        /// <summary>
        /// Creates a dynamic object.
        /// </summary>
        /// <returns></returns>
        public virtual dynamic ToDynamic()
        {
            dynamic expando = new ExpandoObject();
            expando.EntityTypeId = this.EntityTypeId;
            expando.EntityId = this.EntityId;
            expando.Order = this.Order;
            expando.Action = this.Action;
            expando.AllowOrDeny = this.AllowOrDeny;
            expando.SpecialRole = this.SpecialRole;
            expando.PersonId = this.PersonId;
            expando.GroupId = this.GroupId;
            expando.Id = this.Id;
            expando.Guid = this.Guid;
            return expando;
        }

        /// <summary>
        /// Copies the model property values to the DTO properties
        /// </summary>
        /// <param name="model">The model.</param>
        public void CopyFromModel( IEntity model )
        {
            if ( model is Auth )
            {
                var auth = (Auth)model;
                this.EntityTypeId = auth.EntityTypeId;
                this.EntityId = auth.EntityId;
                this.Order = auth.Order;
                this.Action = auth.Action;
                this.AllowOrDeny = auth.AllowOrDeny;
                this.SpecialRole = auth.SpecialRole;
                this.PersonId = auth.PersonId;
                this.GroupId = auth.GroupId;
                this.Id = auth.Id;
                this.Guid = auth.Guid;
            }
        }

        /// <summary>
        /// Copies the DTO property values to the entity properties
        /// </summary>
        /// <param name="model">The model.</param>
        public void CopyToModel ( IEntity model )
        {
            if ( model is Auth )
            {
                var auth = (Auth)model;
                auth.EntityTypeId = this.EntityTypeId;
                auth.EntityId = this.EntityId;
                auth.Order = this.Order;
                auth.Action = this.Action;
                auth.AllowOrDeny = this.AllowOrDeny;
                auth.SpecialRole = this.SpecialRole;
                auth.PersonId = this.PersonId;
                auth.GroupId = this.GroupId;
                auth.Id = this.Id;
                auth.Guid = this.Guid;
            }
        }
    }
}
