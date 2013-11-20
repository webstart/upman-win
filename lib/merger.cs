using System;
using System.IO;
using System.Text.RegularExpressions;


//////////
//  core principle
//   - NEVER copy
//   - NEVER delete
//   -  just move files
//
//
///  minor result =>  allow later restore (should be possible)



public class Merger
{
  string _installRoot;  // application root folder
  string _downloadRoot;
  string _atticRoot;    // "old" updates n patches (might get restored)
  string _trashRoot;    // trash - files and folders no longer used (mostly for used for duplicates that get moved from attic)

  string _packName;  // e.g  meta or pkg or package or paket etc. - where zips and manifest gets stored

  public Merger( string installRoot, string downloadRoot, string atticRoot, string trashRoot )
  {
    // todo: can we call main constructor? - use super() or similar?? possible
    
    _installRoot  = installRoot;   // use appRoot ??
    _downloadRoot = downloadRoot;
    _atticRoot    = atticRoot;
    _trashRoot    = trashRoot;

    _packName  = "paket";

    Console.WriteLine( "merger folders:" );
    Console.WriteLine( "  installRoot:  >"+ _installRoot  + "<" );
    Console.WriteLine( "  downloadRoot: >"+ _downloadRoot + "<" );
    Console.WriteLine( "  atticRoot:    >"+ _atticRoot    + "<" );
    Console.WriteLine( "  trashRoot:    >"+ _trashRoot    + "<" );    
  }

  public Merger( string installRoot )
   : this( installRoot,
           installRoot + @"\downloads",
           installRoot + @"\downloads\attic",
           installRoot + @"\downloads\trash" )
  {
  }

  public void MergeVersionPack( string manifestName )
  {
    string installedVersion = Utils.FindPackVersion( _installRoot+@"\"+_packName+@"\"+ manifestName + ".txt" );

    string versionRoot  = FindVersionPackDir( manifestName );

    if( versionRoot != null )
    {
       Console.WriteLine( "try merge from w/ versionRoot: " + versionRoot ); 

       string dl_updates = versionRoot+@"\updates";
       string dl_patches = versionRoot+@"\patches";

       // make sure dirs exists
       Directory.CreateDirectory( dl_updates ); 
       Directory.CreateDirectory( dl_patches );

       string attic_updates = _atticRoot+@"\"+installedVersion+@"\updates";
       string attic_patches = _atticRoot+@"\"+installedVersion+@"\patches";

       Directory.CreateDirectory( attic_updates );
       Directory.CreateDirectory( attic_patches ); 
          
       HandleUpdates( dl_updates, attic_updates );
       HandlePatches( dl_patches, attic_patches );
    }
  }


  public void MergeLatestPack( string manifestName )
  {
    // string installedVersion = Utils.FindPackVersion( _installRoot+@"\"+_packName+@"\"+ manifestName + ".txt" );

    // todo/fix:
    //  new method!!!!   use FindPackTimestamp()  - assumes no VERSION in manifest
    
    // note: latest assumes no version; will use timestamp of manifestFile for now
    //   roughly equals the download date ??
    string installedVersion = "add_timestamp_here";

    string latestRoot = FindLatestPackDir( manifestName );

    if( latestRoot != null )
    {
       Console.WriteLine( "try merge from w/ latestRoot: " + latestRoot ); 

       string dl_updates = latestRoot+@"\updates";
       string dl_patches = latestRoot+@"\patches";

       // make sure dirs exists
       Directory.CreateDirectory( dl_updates ); 
       Directory.CreateDirectory( dl_patches );

       // use manifestName instead of latest ?? e.g. kv or similar ?? or just merge everything together anyway!!
       string attic_updates = _atticRoot+@"\latest\"+installedVersion+@"\updates";
       string attic_patches = _atticRoot+@"\latest\"+installedVersion+@"\patches";

       Directory.CreateDirectory( attic_updates );
       Directory.CreateDirectory( attic_patches ); 
          
       HandleUpdates( dl_updates, attic_updates );
       HandlePatches( dl_patches, attic_patches );
    }
  }


  string FindLatestPackDir( string manifestName )
  {
    string latestDir = _downloadRoot + @"\latest";
       
    Console.WriteLine( "  check if latest folder exists?" );
    if( Directory.Exists( latestDir ) )
    {
      Console.WriteLine( "  bingo! latest found; checking manifest" );

      string manifestFile = latestDir + @"\" + _packName+ @"\" + manifestName + ".txt";
      Console.WriteLine( "   check if manifest >"+ manifestFile +"< exists?" );
          
      // check if manifestFile exits ??
      //  if yes
      if( File.Exists( manifestFile ) )
      {
        Console.WriteLine( " *** bingo! found manifest" );
        return latestDir;
      }
      else
      {
        return null;  // nothing found
      }
    }
    else
    {
      return null;  // nothing found
    }
  }

  string FindVersionPackDir( string manifestName )
  {
    // walk folders and check
       
    string [] dirs = Directory.GetDirectories( _downloadRoot );
    foreach( string dir in dirs )
    {
       Console.WriteLine( "dir: " + dir );

       // note: relativeFile will start with slash e.g. \weblibs\shared
       string relativeDir = dir.Substring( _downloadRoot.Length, dir.Length-_downloadRoot.Length );   // cut off downloadRoot

       Console.WriteLine( "  relativeDir: " + relativeDir );

       // skip tmp, latest, etc e.g. check if it looks like a version
       // note: we will also skip /latest
       if( Regex.IsMatch( relativeDir, "[0-9]+\\.[0-9]+" ))    // todo: use anchor? use case insensitivee?
       {
          Console.WriteLine( "** bingo - versioned folder found; checking if manifest exists" );

          string manifestFile = dir + @"\" + _packName + @"\" + manifestName + ".txt";
          Console.WriteLine( "   check if manifest >"+ manifestFile +"< exists?" );
          
          // check if manifestFile exits ??
          //  if yes
          if( File.Exists( manifestFile ) )
          {
            Console.WriteLine( " *** bingo! found manifest" );
            return dir;
          }
      }
    }

    return null;   // nothing found; sorry
  }


    void HandlePatches( string patchesRoot, string atticRoot )
    {
        // note: in theory - patches should only be a handful of files (ideally)
        //   - do NOT abuse for switching complete webapps w/ hundreds of files and subfolders
        //   - NOT idealy - patches do NOT include subfolders (works for now - but may be forbidden in the future??  keep it simple!)

        HandlePatchesWorker( patchesRoot, patchesRoot, atticRoot, 1 );
    }


    void HandlePatchesWorker( string root, string patchesRoot, string atticRoot, int level )
    {
       string [] dirs = Directory.GetDirectories( root );
       foreach( string dir in dirs )
       {
          HandlePatchesWorker( dir, patchesRoot, atticRoot, level+1 );
       }

       string [] files = Directory.GetFiles( root );
       foreach( string file in files )
       {
          HandlePatch( file, patchesRoot, atticRoot );
       }
    }



    void HandleUpdates( string updatesRoot, string atticRoot )
    {
       HandleUpdatesWorker( updatesRoot, updatesRoot, atticRoot, 1 );
    }

    void HandleUpdatesWorker( string root, string updatesRoot, string atticRoot, int level )
    {
       string [] dirs = Directory.GetDirectories( root );
       foreach( string dir in dirs )
       {
          if( Utils.IsDirFlat( dir ) || Utils.IsDirFull( dir ) || Utils.IsDirJavaWebArchive( dir ) )
          {
            HandleUpdate( dir, updatesRoot, atticRoot );
          }
          else
          {  // assume empty directory (no files) - just used for navigation; continue
            Console.WriteLine( "  skip folder in updates >" + dir + "<; continue walking" );
            HandleUpdatesWorker( dir, updatesRoot, atticRoot, level+1 );
          }
       }
    }


    void HandlePatch( string file, string patchesRoot, string atticRoot )
    {
       Console.WriteLine( "process file: " + file );

       // note: relativeFile will start with slash e.g. \weblibs\private
       string relativeFile = file.Substring( patchesRoot.Length, file.Length-patchesRoot.Length );   // cut off patchesRoot

       Console.WriteLine( "  relative: " + relativeFile );

       string installFile = _installRoot + relativeFile;
       Console.WriteLine( "  installFile: " + installFile );
      
       string atticFile = atticRoot + relativeFile;
       Console.WriteLine( "  atticFile: " + atticFile );

       // if file exits already - move it to attic
       // -- if file also exits in attic; move it to trash
       if( File.Exists( installFile ) )
       {
         if( File.Exists( atticFile ))
         {
           // use a flat folder structure in trash
           string ts = DateTime.Now.ToString( "yyyy-MM-dd_HH-mm-ss.fff" );
           string trashRelativeFlatFile = relativeFile.Replace( @"\", "__I__" );
           string trashFile = _trashRoot + @"\__" + ts + "__" + trashRelativeFlatFile + ".tmp";

           // make sure dirs exists
           DirectoryInfo trashDirInfo = new FileInfo( trashFile ).Directory;
           Console.WriteLine( "  trashDirInfo: " + trashDirInfo.Name );
           trashDirInfo.Create(); //  make sure parent dirs exists (create if not)

           Console.WriteLine( "move to trash - " + atticFile + " => " + trashFile );

           // todo: add flag for dry run!!!
           File.Move( atticFile, trashFile );
         }

         DirectoryInfo atticDirInfo = new FileInfo( atticFile ).Directory;
         Console.WriteLine( "  atticDirInfo: " + atticDirInfo.Name );
         atticDirInfo.Create(); //  make sure parent dirs exists (create if not)
            
         Console.WriteLine( "move to attic - " + installFile + " => " + atticFile );
            
         // todo: add flag for dry run!!!
         File.Move( installFile, atticFile );
        }
          
        DirectoryInfo installDirInfo = new FileInfo( installFile ).Directory;
        Console.WriteLine( "  installDirInfo: " + installDirInfo.Name );
        installDirInfo.Create(); //  make sure parent dirs exists (create if not)

        Console.WriteLine( "move update to install - " + file + " => " + installFile );

        // todo: add flag for dry run!!!
        File.Move( file, installFile );
    }

    void HandleUpdate( string dir, string updatesRoot, string atticRoot )
    {
          Console.WriteLine( "process dir: " + dir );
          
          // note: relativeDir will start with slash e.g. \weblibs\private
          string relativeFlatDir = dir.Substring( updatesRoot.Length, dir.Length-updatesRoot.Length );   // cut off updatesRoot
          // todo: check if it works w/ multiple replaces (works like gsub not sub) ???
          string relativeDir =  relativeFlatDir.Replace( "__I__", @"\" );     // replace __I__ with proper folder separator, that is, => \

          Console.WriteLine( "  relativeFlat: " + relativeFlatDir );
          Console.WriteLine( "  relative: " + relativeDir );

          string installDir = _installRoot + relativeDir;
          Console.WriteLine( "  installDir: " + installDir );
          
          string atticDir = atticRoot + relativeFlatDir;
          Console.WriteLine( "  atticDir: " + atticDir );

          // if folder exits already - move it to attic
          // -- if folder also exits in attic; move it to trash
          if( Directory.Exists( installDir ) )
          {
            if( Directory.Exists( atticDir ))
            {
               string ts = DateTime.Now.ToString( "yyyy-MM-dd_HH-mm-ss.fff" );

               // use a flat folder structure in trash
               string trashRelativeFlatDir = relativeDir.Replace( @"\", "__I__" );
               string trashDir = _trashRoot + @"\__" + ts + "__" + trashRelativeFlatDir + ".tmp";

               // make sure dirs exists
               DirectoryInfo trashDirInfo = Directory.GetParent( trashDir );
               Console.WriteLine( "  trashDirInfo: " + trashDirInfo.Name );
               trashDirInfo.Create(); //  make sure parent dirs exists (create if not)
               
               // todo: add flag for dry run!!!
               Console.WriteLine( "move to trash - " + atticDir + " => " + trashDir );
               Directory.Move( atticDir, trashDir );
            }

            DirectoryInfo atticParentDirInfo = Directory.GetParent( atticDir );
            atticParentDirInfo.Create(); //  make sure parent dirs exists (create if not)
            
            Console.WriteLine( "move to attic - " + installDir + " => " + atticDir );
            
            // todo: add flag for dry run!!!
            Directory.Move( installDir, atticDir );
          }
          
          DirectoryInfo installParentDirInfo = Directory.GetParent( installDir );
          installParentDirInfo.Create(); //  make sure parent dirs exists (create if not)

          Console.WriteLine( "move update to install - " + dir + " => " + installDir );

          // todo: add flag for dry run!!!
          Directory.Move( dir, installDir );
    }
}  // class Merger

