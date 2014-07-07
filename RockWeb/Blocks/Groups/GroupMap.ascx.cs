﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Data.Entity.Spatial;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using System.Text;
using System.Collections.Generic;
using DotLiquid;
using System.Dynamic;
using Rock.Web;

namespace RockWeb.Blocks.Groups
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Group Map" )]
    [Category( "Groups" )]
    [Description( "Displays a group (and any child groups) on a map." )]

    [IntegerField( "Map Height", "Height of the map in pixels (default value is 600px)", false, 600, "", 2 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.MAP_STYLES, "Map Style", "The map theme that should be used for styling the map.", true, false, Rock.SystemGuid.DefinedValue.MAP_STYLE_GOOGLE, "", 8 )]
    [TextField( "Polygon Colors", "Comma-Delimited list of colors to use when displaying multiple polygons (e.g. F71E22,E75C1F,E7801,F7A11F).", true, "F71E22,E75C1F,E7801,F7A11F" )]
    public partial class GroupMap : Rock.Web.UI.RockBlock
    {
        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            lMessages.Text = string.Empty;
            pnlMap.Visible = true;

            if ( !Page.IsPostBack )
            {
                rptStatus.DataSource = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS.AsGuid() ).DefinedValues
                    .OrderBy( v => v.Order )
                    .ThenBy( v => v.Name )
                    .Select( v => new
                    {
                        v.Id,
                        Name = v.Name.Pluralize()
                    } )
                    .ToList();
                rptStatus.DataBind();

                Map();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the GroupMapper control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            pnlMap.Visible = true;
            Map();
        }

        #endregion

        #region Methods

        private void Map()
        {
            int? groupId = PageParameter( "GroupId" ).AsIntegerOrNull();
            if ( !groupId.HasValue )
            {
                pnlMap.Visible = false;
                lMessages.Text = "<div class='alert alert-warning'><strong>Group Map</strong> A Group ID is required to display the map.</div>";
                return;
            }
            
            pnlMap.Visible = true;

            string mapStylingFormat = @"
                        <style>
                            #map_wrapper {{
                                height: {0}px;
                            }}

                            #map_canvas {{
                                width: 100%;
                                height: 100%;
                                border-radius: 8px;
                            }}
                        </style>";
            lMapStyling.Text = string.Format( mapStylingFormat, GetAttributeValue( "MapHeight" ) );

            // add styling to map
            string styleCode = "null";
            string markerColor = "FE7569";

            DefinedValueCache dvcMapStyle = DefinedValueCache.Read( GetAttributeValue( "MapStyle" ).AsGuid() );
            if ( dvcMapStyle != null )
            {
                styleCode = dvcMapStyle.GetAttributeValue( "DynamicMapStyle" );
                markerColor = dvcMapStyle.GetAttributeValue( "MarkerColor" ).Replace( "#", string.Empty );
            }

            var polygonColorList = GetAttributeValue( "PolygonColors" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            string polygonColors = "\"" + polygonColorList.AsDelimited( "\", \"" ) + "\"";

            // write script to page
            string mapScriptFormat = @"
<script> 

    Sys.Application.add_load(function () {{

        var groupId = {0};
        var groupItems = [];
        var childGroupItems;
        var groupMemberItems
        var familyItems = {{}};

        var map;
        var bounds = new google.maps.LatLngBounds();
        var infoWindow = new google.maps.InfoWindow();

        var mapStyle = {1};
        var pinColor = '{2}';
        var pinImage = new google.maps.MarkerImage('http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=%E2%80%A2|' + pinColor,
            new google.maps.Size(21, 34),
            new google.maps.Point(0,0),
            new google.maps.Point(10, 34));
        var pinShadow = new google.maps.MarkerImage('http://chart.apis.google.com/chart?chst=d_map_pin_shadow',
            new google.maps.Size(40, 37),
            new google.maps.Point(0, 0),
            new google.maps.Point(12, 35));

        var polygonColorIndex = 0;
        var polygonColors = [{3}];

        initializeMap();

        function initializeMap() {{

            var mapOptions = {{
                 mapTypeId: 'roadmap'
                ,styles: mapStyle
                ,center: new google.maps.LatLng(39.8282, -98.5795)
                ,zoom: 4
            }};

            // Display a map on the page
            map = new google.maps.Map(document.getElementById('map_canvas'), mapOptions);
            map.setTilt(45);

            $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/{0}', function( mapItems ) {{

                // Loop through array of map items
                $.each(mapItems, function (i, mapItem) {{
                    $('#lGroupName').text(mapItem.Name);
                    var items = addMapItem(i, mapItem, {0});
                    for (var i = 0; i < items.length; i++) {{
                        groupItems.push(items[i]);
                    }}
                }});

                if (groupItems.length == 0) {{
                    getChildGroups();
                    $('.js-show-hide-options').hide();
                }}

                if (!bounds.isEmpty()) {{
                    map.fitBounds(bounds);
                }}
                       
            }});    

        }}

        function addMapItem( i, mapItem ) {{

            var items = [];

            if (mapItem.Point) {{ 

                var position = new google.maps.LatLng(mapItem.Point.Latitude, mapItem.Point.Longitude);
                bounds.extend(position);

                marker = new google.maps.Marker({{
                    position: position,
                    map: map,
                    title: htmlDecode(mapItem.Name),
                    icon: pinImage,
                    shadow: pinShadow
                }});
    
                items.push(marker);

                google.maps.event.addListener(marker, 'click', (function (marker, i) {{
                    return function () {{
                        infoWindow.setContent(mapItem.Name);
                        infoWindow.open(map, marker);
                    }}
                }})(marker, i));

            }}

            if (typeof mapItem.PolygonPoints !== 'undefined' && mapItem.PolygonPoints.length > 0) {{

                var polygon;
                var polygonPoints = [];

                $.each(mapItem.PolygonPoints, function(j, point) {{
                    var position = new google.maps.LatLng(point.Latitude, point.Longitude);
                    bounds.extend(position);
                    polygonPoints.push(position);
                }});

                var polygonColor = getNextPolygonColor();

                polygon = new google.maps.Polygon({{
                    paths: polygonPoints,
                    map: map,
                    strokeColor: polygonColor,
                    fillColor: polygonColor
                }});

                items.push(polygon);

                // Get Center
                var polyBounds = new google.maps.LatLngBounds();
                for ( j = 0; j < polygonPoints.length; j++) {{
                    polyBounds.extend(polygonPoints[j]);
                }}

                google.maps.event.addListener(polygon, 'click', (function (polygon, i) {{
                    return function () {{
                        infoWindow.setContent(mapItem.Name);
                        infoWindow.setPosition(polyBounds.getCenter());
                        infoWindow.open(map);
                    }}
                }})(polygon, i));

                if ( mapItem.EntityId == {0} ) {{
                    $('.js-connection-status-cb').closest('.form-group').show();
                }}
        
            }}

            return items;

        }}
        
        // Show/Hide group
        $('#cbShowGroup').click( function() {{
            if ($(this).prop('checked')) {{
                setAllMap(groupItems, map);
            }} else {{
                setAllMap(groupItems, null);
            }} 
        }});

        // Show/Hide child groups
        $('#cbShowChildGroups').click( function() {{
            if ($(this).prop('checked')) {{
                if (typeof childGroupItems !== 'undefined') {{
                    setAllMap(childGroupItems, map);
                }} else {{
                    getChildGroups();
                }}
            }} else {{
                if (typeof childGroupItems !== 'undefined') {{
                    setAllMap(childGroupItems, null);
                }} 
            }}
        }});

        function getChildGroups() {{
            childGroupItems = [];
            $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/{0}/Children', function( mapItems ) {{
                $.each(mapItems, function (i, mapItem) {{
                    var items = addMapItem(i, mapItem);
                    for (var i = 0; i < items.length; i++) {{
                        childGroupItems.push(items[i]);
                    }}
                }});
                map.fitBounds(bounds);
            }});
        }}

        // Show/Hide group members
        $('#cbShowGroupMembers').click( function() {{
            if ($(this).prop('checked')) {{
                if (typeof groupMemberItems !== 'undefined') {{
                    setAllMap(groupMemberItems, map);
                }} else {{
                    groupMemberItems = [];
                    $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/{0}/Members', function( mapItems ) {{
                        $.each(mapItems, function (i, mapItem) {{
                            var items = addMapItem(i, mapItem);
                            for (var i = 0; i < items.length; i++) {{
                                groupMemberItems.push(items[i]);
                            }}
                        }});
                        map.fitBounds(bounds);
                    }});
                }}
            }} else {{
                if (typeof groupMemberItems !== 'undefined') {{
                    setAllMap(groupMemberItems, null);
                }} 
            }}
        }});

        // Show/Hide families
        $('.js-connection-status-cb').click( function() {{
            var statusId = $(this).attr('data-item');
            if ($(this).prop('checked')) {{
                if (typeof familyItems[statusId] !== 'undefined') {{
                    setAllMap(familyItems[statusId], map);
                }} else {{
                    familyItems[statusId] = [];
                    $.get( Rock.settings.get('baseUrl') + 'api/Groups/GetMapInfo/{0}/Families/' + statusId, function( mapItems ) {{
                        $.each(mapItems, function (i, mapItem) {{
                            var items = addMapItem(i, mapItem);
                            for (var i = 0; i < items.length; i++) {{
                                familyItems[statusId].push(items[i]);
                            }}
                        }});
                        map.fitBounds(bounds);
                    }});
                }}
            }} else {{
                if (typeof familyItems[statusId] !== 'undefined') {{
                    setAllMap(familyItems[statusId], null);
                }} 
            }}
        }});

        function setAllMap(markers, map) {{
            for (var i = 0; i < markers.length; i++) {{
                markers[i].setMap(map);
            }}
        }}

        function htmlDecode(input) {{
            var e = document.createElement('div');
            e.innerHTML = input;
            return e.childNodes.length === 0 ? """" : e.childNodes[0].nodeValue;
        }}

        function getNextPolygonColor() {{
            var color = 'FE7569';
            if ( polygonColors.length > polygonColorIndex ) {{
                color = polygonColors[polygonColorIndex];
                polygonColorIndex++;
            }} else {{
                color = polygonColors[0];
                polygonColorIndex = 1;
            }}
            return color;
        }}

    }});
</script>";

            string mapScript = string.Format( mapScriptFormat, groupId.Value, styleCode, markerColor, polygonColors );

            ScriptManager.RegisterStartupScript( pnlMap, pnlMap.GetType(), "group-map-script", mapScript, false );

        }


        #endregion
    }
}