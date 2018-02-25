﻿/*
 * Copyright © 2016 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace EliteDangerousCore.DB
{
    public class PlanetMarks
    {
        public class Location
        {
            public string Name;
            public string Comment;
            public double Latitude;
            public double Longitude;
        }

        public class Planet
        {
            public string Name;
            public List<Location> Locations;
        }

        public List<Planet> Planets;

        public PlanetMarks(string json)
        {
            try // prevent crashes
            {
                JObject jo = JObject.Parse(json);
                if (jo["Marks"] != null)
                {
                    Planets = jo["Marks"].ToObject<List<Planet>>();
                }
            }
            catch
            { }
        }

        public PlanetMarks()
        {
        }

        public string ToJsonString()
        {
            if (Planets != null)
            {
                JArray ja = new JArray();
                foreach (Planet p in Planets)
                    ja.Add(JObject.FromObject(p));

                JObject overall = new JObject();
                overall["Marks"] = ja;
                return overall.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            else
                return null;
        }

        public Planet GetPlanet(string planet)  // null if planet does not exist.. else array
        {
            return Planets?.Find(x => x.Name.Equals(planet, StringComparison.InvariantCultureIgnoreCase));
        }

        public Location GetLocation(Planet p, string placename)  // null if planet or place does not exist..
        {
            return p?.Locations?.Find(x => x.Name.Equals(placename, StringComparison.InvariantCultureIgnoreCase));
        }

        public void AddOrUpdateLocation(string planet, string placename, string comment, double latp, double longp)
        {
            Planet p = GetPlanet(planet);            // p = null if planet does not exist, else list of existing places

            if (p == null)      // no planet, make one up
            {
                if (Planets == null)
                    Planets = new List<Planet>();       // new planet list

                p = new Planet() { Name = planet, Locations = new List<Location>() };
                Planets.Add(p);
            }

            Location l = GetLocation(p, placename);     // location on planet by name

            if (l == null)                      // no location.. make one up and add
            {
                l = new Location() { Name = placename, Comment = comment, Latitude = latp, Longitude = longp };
                p.Locations.Add(l);
            }
            else
            {
                l.Comment = comment;        // update fields which may have changed
                l.Latitude = latp;
                l.Longitude = longp;
            }
        }

        public void AddOrUpdateLocation(string planet, Location loc)
        {
            AddOrUpdateLocation(planet, loc.Name, loc.Comment, loc.Latitude, loc.Longitude);
        }

        public bool DeleteLocation(string planet, string placename)
        {
            Planet p = GetPlanet(planet);            // p = null if planet does not exist, else list of existing places
            Location l = GetLocation(p, placename); // if p != null, find placenameYour okay, its 
            if (l != null)
            {
                p.Locations.Remove(l);
                if (p.Locations.Count == 0) // nothing left?
                    Planets.Remove(p);  // remove planet.
            }
            return l != null;
        }

        public bool HasLocation(string planet, string placename)
        {
            Planet p = GetPlanet(planet);            // p = null if planet does not exist, else list of existing places
            Location l = GetLocation(p, placename); // if p != null, find placenameYour okay, its 
            return l != null;
        }

        public bool UpdateComment(string planet, string placename, string comment)
        {
            Planet p = GetPlanet(planet);            // p = null if planet does not exist, else list of existing places
            Location l = GetLocation(p, placename); // if p != null, find placenameYour okay, its 
            if (l != null)
            {
                l.Comment = comment;
                return true;
            }
            else
                return false;
        }
    }
        
    public class BookmarkClass
    {
        public long id;
        public string StarName;         // set if associated with a star, else null
        public double x;                // x/y/z always set for render purposes
        public double y;
        public double z;
        public DateTime Time;           
        public string Heading;          // set if region bookmark, else null if its a star
        public string Note;
        public PlanetMarks PlanetaryMarks;   // may be null
        
        public bool isRegion { get { return Heading != null; } }
        public bool hasSurfaceMarks
        { get {
                return PlanetaryMarks != null &&
                    PlanetaryMarks.Planets.Count > 0 &&
                    PlanetaryMarks.Planets.Where(pl => pl.Locations.Count > 0).Any();
            } }
        public bool isStar { get { return Heading == null; } }
        public string Name { get { return Heading == null ? StarName : Heading; } }

        public BookmarkClass()
        {
        }

        public BookmarkClass(DataRow dr)
        {
            id = (long)dr["id"];
            if (System.DBNull.Value != dr["StarName"])
                StarName = (string)dr["StarName"];
            x = (double)dr["x"];
            y = (double)dr["y"];
            z = (double)dr["z"];
            Time = (DateTime)dr["Time"];
            if (System.DBNull.Value != dr["Heading"])
                Heading = (string)dr["Heading"];
            Note = (string)dr["Note"];
            if (System.DBNull.Value != dr["PlanetMarks"])
            {
                PlanetaryMarks = new PlanetMarks((string)dr["PlanetMarks"]);
            }
        }

        internal bool Add()
        {
            using (SQLiteConnectionUser cn = new SQLiteConnectionUser())      // open connection..
            {
                return Add(cn);
            }
        }

        private bool Add(SQLiteConnectionUser cn)
        {
            using (DbCommand cmd = cn.CreateCommand("Insert into Bookmarks (StarName, x, y, z, Time, Heading, Note, PlanetMarks) values (@sname, @xp, @yp, @zp, @time, @head, @note, @pmarks)"))
            {
                cmd.AddParameterWithValue("@sname", StarName);
                cmd.AddParameterWithValue("@xp", x);
                cmd.AddParameterWithValue("@yp", y);
                cmd.AddParameterWithValue("@zp", z);
                cmd.AddParameterWithValue("@time", Time);
                cmd.AddParameterWithValue("@head", Heading);
                cmd.AddParameterWithValue("@note", Note);
                cmd.AddParameterWithValue("@pmarks", PlanetaryMarks?.ToJsonString());

                SQLiteDBClass.SQLNonQueryText(cn, cmd);

                using (DbCommand cmd2 = cn.CreateCommand("Select Max(id) as id from Bookmarks"))
                {
                    id = (long)SQLiteDBClass.SQLScalar(cn, cmd2);
                }

                GlobalBookMarkList.Add(this);
                return true;
            }
        }

        internal bool Update()
        {
            using (SQLiteConnectionUser cn = new SQLiteConnectionUser())
            {
                return Update(cn);
            }
        }

        private bool Update(SQLiteConnectionUser cn)
        {
            using (DbCommand cmd = cn.CreateCommand("Update Bookmarks set StarName=@sname, x = @xp, y = @yp, z = @zp, Time=@time, Heading = @head, Note=@note, PlanetMarks=@pmarks  where ID=@id"))
            {
                cmd.AddParameterWithValue("@ID", id);
                cmd.AddParameterWithValue("@sname", StarName);
                cmd.AddParameterWithValue("@xp", x);
                cmd.AddParameterWithValue("@yp", y);
                cmd.AddParameterWithValue("@zp", z);
                cmd.AddParameterWithValue("@time", Time);
                cmd.AddParameterWithValue("@head", Heading);
                cmd.AddParameterWithValue("@note", Note);
                cmd.AddParameterWithValue("@pmarks", PlanetaryMarks?.ToJsonString());

                SQLiteDBClass.SQLNonQueryText(cn, cmd);

                GlobalBookMarkList.RemoveAll(x => x.id == id, true);     // remove from list any containing id.
                GlobalBookMarkList.Add(this);

                return true;
            }
        }

        public bool Delete()
        {
            using (SQLiteConnectionUser cn = new SQLiteConnectionUser())
            {
                return Delete(cn);
            }
        }

        private bool Delete(SQLiteConnectionUser cn)
        {
            using (DbCommand cmd = cn.CreateCommand("DELETE FROM Bookmarks WHERE id = @id"))
            {
                cmd.AddParameterWithValue("@id", id);
                SQLiteDBClass.SQLNonQueryText(cn, cmd);

                GlobalBookMarkList.RemoveAll(x => x.id == id, false);     // remove from list any containing id.
                return true;
            }
        }
        
        // with a found bookmark.. add locations in the system
        public void AddOrUpdateLocation(string planet, string placename, string comment, double latp, double longp)
        {
            if (PlanetaryMarks == null)
                PlanetaryMarks = new PlanetMarks();
            PlanetaryMarks.AddOrUpdateLocation(planet, placename, comment, latp, longp);
            Update();
        }
        
		// Update notes
        public void UpdateNotes(string notes)
        {
            Note = notes;
            Update();
        }
        
        public bool HasLocation(string planet, string placename)
        {
            return PlanetaryMarks != null && PlanetaryMarks.HasLocation(planet, placename);
        }

        public bool DeleteLocation(string planet, string placename)
        {
            if (PlanetaryMarks != null && PlanetaryMarks.DeleteLocation(planet, placename))
            {
                Update();
                return true;
            }
            else
                return false;
        }

        public bool UpdateLocationComment(string planet, string placename, string comment)
        {
            if (PlanetaryMarks != null && PlanetaryMarks.UpdateComment(planet, placename,comment))
            {
                Update();
                return true;
            }
            else
                return false;
        }
    }

    public delegate void GlobalBookmarkRefresh();
    public delegate void GlobalBookmarkChange(long bookMarkID);
    public delegate void GlobalBookmarkRemoved(Predicate<BookmarkClass> predicate);

    public static class GlobalBookMarkList
    {
        private static List<BookmarkClass> globalbookmarks = new List<BookmarkClass>();

        public static List<BookmarkClass> Bookmarks { get { return globalbookmarks; } }
        public static event GlobalBookmarkRefresh OnBookmarkRefresh;
        public static event GlobalBookmarkChange OnBookmarkChange;
        public static event GlobalBookmarkRemoved OnBookmarkRemoved;

        public static void Clear()
        {
            globalbookmarks.Clear();
            OnBookmarkRefresh?.Invoke();
        }

        public static void Add(BookmarkClass newBookmark)
        {
            globalbookmarks.Add(newBookmark);
            OnBookmarkChange?.Invoke(newBookmark.id);
        }

        public static void FireRefresh()
        {
            OnBookmarkRefresh?.Invoke();
        }

        internal static void RemoveAll(Predicate<BookmarkClass> predicate, bool updating)
        {
            globalbookmarks.RemoveAll(predicate);
            if (!updating)
                OnBookmarkRemoved?.Invoke(predicate);
        }
        // return any mark
        public static BookmarkClass FindBookmarkOnRegion(string name)   
        {
            return globalbookmarks.Find(x => x.Heading != null && x.Name.Equals(x.Heading, StringComparison.InvariantCultureIgnoreCase));
        }

        public static BookmarkClass FindBookmarkOnSystem(string name)
        {
            // star name may be null if its a region mark
            return globalbookmarks.Find(x => x.StarName != null && x.StarName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
        public static BookmarkClass FindBookmark(string name , bool region)
        {
            // star name may be null if its a region mark
            return (region) ? FindBookmarkOnRegion(name) : FindBookmarkOnSystem(name);
        }

        // on a star system, if an existing bookmark, return it, else create a new one with these properties
        public static BookmarkClass EnsureBookmarkOnSystem(string name, double x, double y, double z, DateTime tme, string notes = null)
        {
            BookmarkClass bk = FindBookmarkOnSystem(name);
            return bk != null ? bk : AddOrUpdateBookmark(null, true, name, x, y, z, tme, notes);
        }

        // bk = null, new bookmark, else update.  isstar = true, region = false.
        public static BookmarkClass AddOrUpdateBookmark(BookmarkClass bk, bool isstar, string name, double x, double y, double z, DateTime tme, string notes = null, PlanetMarks planetMarks = null)
        {
            bool addit = bk == null;

            if (bk == null)
            {
                bk = new BookmarkClass();
                bk.Note = "";       // set empty, in case notes==null
            }

            if (isstar)
                bk.StarName = name;
            else
                bk.Heading = name;

            bk.x = x;
            bk.y = y;
            bk.z = z;
            bk.Time = tme;
            bk.PlanetaryMarks = planetMarks ?? bk.PlanetaryMarks;
            bk.Note = notes ?? bk.Note; // only override notes if its set.

            if (addit)
                bk.Add();
            else
                bk.Update();

            OnBookmarkChange?.Invoke(bk.id);

            return bk;
		}	

        public static bool LoadBookmarks()
        {
            try
            {
                using (SQLiteConnectionUser cn = new SQLiteConnectionUser(mode: EDDbAccessMode.Reader))
                {
                    using (DbCommand cmd = cn.CreateCommand("select * from Bookmarks"))
                    {
                        DataSet ds = null;

                        ds = SQLiteDBClass.SQLQueryText(cn, cmd);

                        if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                        {
                            return false;
                        }

                        globalbookmarks.Clear();

                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            BookmarkClass bc = new BookmarkClass(dr);
                            globalbookmarks.Add(bc);
                        }
                        OnBookmarkRefresh?.Invoke();

                        return true;

                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
