<!DOCTYPE html>
<!--[if IE]><![endif]-->
<html>
  
  <head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
    <title>Class RecipeWeightInfo
   </title>
    <meta name="viewport" content="width=device-width">
    <meta name="title" content="Class RecipeWeightInfo
   ">
    <meta name="generator" content="docfx 2.59.4.0">
    
    <link rel="shortcut icon" href="../favicon.ico">
    <link rel="stylesheet" href="../styles/docfx.vendor.css">
    <link rel="stylesheet" href="../styles/docfx.css">
    <link rel="stylesheet" href="../styles/main.css">
    <meta property="docfx:navrel" content="../toc.html">
    <meta property="docfx:tocrel" content="toc.html">
    
    
    
  </head>
  <body data-spy="scroll" data-target="#affix" data-offset="120">
    <div id="wrapper">
      <header>
        
        <nav id="autocollapse" class="navbar navbar-inverse ng-scope" role="navigation">
          <div class="container">
            <div class="navbar-header">
              <button type="button" class="navbar-toggle" data-toggle="collapse" data-target="#navbar">
                <span class="sr-only">Toggle navigation</span>
                <span class="icon-bar"></span>
                <span class="icon-bar"></span>
                <span class="icon-bar"></span>
              </button>
              
              <a class="navbar-brand" href="../index.html">
                <img id="logo" class="svg" src="../logo.svg" alt="">
              </a>
            </div>
            <div class="collapse navbar-collapse" id="navbar">
              <form class="navbar-form navbar-right" role="search" id="search">
                <div class="form-group">
                  <input type="text" class="form-control" id="search-query" placeholder="Search" autocomplete="off">
                </div>
              </form>
            </div>
          </div>
        </nav>
        
        <div class="subnav navbar navbar-default">
          <div class="container hide-when-search" id="breadcrumb">
            <ul class="breadcrumb">
              <li></li>
            </ul>
          </div>
        </div>
      </header>
      <div role="main" class="container body-content hide-when-search">
        
        <div class="sidenav hide-when-search">
          <a class="btn toc-toggle collapse" data-toggle="collapse" href="#sidetoggle" aria-expanded="false" aria-controls="sidetoggle">Show / Hide Table of Contents</a>
          <div class="sidetoggle collapse" id="sidetoggle">
            <div id="sidetoc"></div>
          </div>
        </div>
        <div class="article row grid-right">
          <div class="col-md-10">
            <article class="content wrap" id="_content" data-uid="Database.RecipeWeightInfo">
  
  
  <h1 id="Database_RecipeWeightInfo" data-uid="Database.RecipeWeightInfo" class="text-break">Class RecipeWeightInfo
  </h1>
  <div class="markdown level0 summary"><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="2">Class containing the infomration of the recipe weight database table. The table must contain at least the following colummns:
id,
nextSeqType,
nextSeqId,
seqName,
isBarcodeUsed,
barcode,
unit,
decimalNumber,
setpoint,
min,
max</p>
<p>Creation revision: 001</p>
</div>
  <div class="markdown level0 conceptual"></div>
  <div class="inheritance">
    <h5>Inheritance</h5>
    <div class="level0"><span class="xref">System.Object</span></div>
    <div class="level1"><span class="xref">RecipeWeightInfo</span></div>
  </div>
  <div class="implements">
    <h5>Implements</h5>
    <div><a class="xref" href="Database.ISeqTabInfo.html">ISeqTabInfo</a></div>
    <div><a class="xref" href="Database.IComTabInfo.html">IComTabInfo</a></div>
    <div><a class="xref" href="Database.IBasTabInfo.html">IBasTabInfo</a></div>
  </div>
  <div class="inheritedMembers">
    <h5>Inherited Members</h5>
    <div>
      <span class="xref">System.Object.ToString()</span>
    </div>
    <div>
      <span class="xref">System.Object.Equals(System.Object)</span>
    </div>
    <div>
      <span class="xref">System.Object.Equals(System.Object, System.Object)</span>
    </div>
    <div>
      <span class="xref">System.Object.ReferenceEquals(System.Object, System.Object)</span>
    </div>
    <div>
      <span class="xref">System.Object.GetHashCode()</span>
    </div>
    <div>
      <span class="xref">System.Object.GetType()</span>
    </div>
    <div>
      <span class="xref">System.Object.MemberwiseClone()</span>
    </div>
  </div>
  <h6><strong>Namespace</strong>: <a class="xref" href="Database.html">Database</a></h6>
  <h6><strong>Assembly</strong>: Database.dll</h6>
  <h5 id="Database_RecipeWeightInfo_syntax">Syntax</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public class RecipeWeightInfo : ISeqTabInfo, IComTabInfo, IBasTabInfo</code></pre>
  </div>
  <h5 id="Database_RecipeWeightInfo_remarks"><strong>Remarks</strong></h5>
  <div class="markdown level0 remarks"><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">This table contains the required information to perform cycle weight sequences</p>
</div>
  <h3 id="constructors">Constructors
  </h3>
  
  
  <a id="Database_RecipeWeightInfo__ctor_" data-uid="Database.RecipeWeightInfo.#ctor*"></a>
  <h4 id="Database_RecipeWeightInfo__ctor" data-uid="Database.RecipeWeightInfo.#ctor">RecipeWeightInfo()</h4>
  <div class="markdown level1 summary"><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="2">Sets all the variables of the class except the values of the variable Columns</p>
</div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public RecipeWeightInfo()</code></pre>
  </div>
  <h3 id="properties">Properties
  </h3>
  
  
  <a id="Database_RecipeWeightInfo_Barcode_" data-uid="Database.RecipeWeightInfo.Barcode*"></a>
  <h4 id="Database_RecipeWeightInfo_Barcode" data-uid="Database.RecipeWeightInfo.Barcode">Barcode</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Barcode { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the barcode column. This column contains the value of the barcode to be controlled</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Columns_" data-uid="Database.RecipeWeightInfo.Columns*"></a>
  <h4 id="Database_RecipeWeightInfo_Columns" data-uid="Database.RecipeWeightInfo.Columns">Columns</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public List&lt;Column&gt; Columns { get; set; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Collections.Generic.List</span>&lt;<a class="xref" href="Database.Column.html">Column</a>&gt;</td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Columns of the database table. From IBasTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Criteria_" data-uid="Database.RecipeWeightInfo.Criteria*"></a>
  <h4 id="Database_RecipeWeightInfo_Criteria" data-uid="Database.RecipeWeightInfo.Criteria">Criteria</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Criteria { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the min column. This column contains the minimum acceptable weight by unit of final product</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_DecimalNumber_" data-uid="Database.RecipeWeightInfo.DecimalNumber*"></a>
  <h4 id="Database_RecipeWeightInfo_DecimalNumber" data-uid="Database.RecipeWeightInfo.DecimalNumber">DecimalNumber</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int DecimalNumber { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the decimal number column. This column contains the number of decimal places to be displays for the setpoint, min, max and value of the weight during the cycle sequence</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Id_" data-uid="Database.RecipeWeightInfo.Id*"></a>
  <h4 id="Database_RecipeWeightInfo_Id" data-uid="Database.RecipeWeightInfo.Id">Id</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Id { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the id column (usually the first one: 0). This column <code>must be</code> an integer, usually automatically incremented. From IComTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Ids_" data-uid="Database.RecipeWeightInfo.Ids*"></a>
  <h4 id="Database_RecipeWeightInfo_Ids" data-uid="Database.RecipeWeightInfo.Ids">Ids</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public string[] Ids { get; set; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.String</span>[]</td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Name of the columns of the database table. From IBasTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_IsBarcodeUsed_" data-uid="Database.RecipeWeightInfo.IsBarcodeUsed*"></a>
  <h4 id="Database_RecipeWeightInfo_IsBarcodeUsed" data-uid="Database.RecipeWeightInfo.IsBarcodeUsed">IsBarcodeUsed</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int IsBarcodeUsed { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the is barcode column. This column informs if the barcode of the product needs to be controlled during the cycle sequence</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_IsSolvent_" data-uid="Database.RecipeWeightInfo.IsSolvent*"></a>
  <h4 id="Database_RecipeWeightInfo_IsSolvent" data-uid="Database.RecipeWeightInfo.IsSolvent">IsSolvent</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int IsSolvent { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the is solvent column. This column if the product is a solvent (if it must be evaporated at the end of the cycle)</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Name_" data-uid="Database.RecipeWeightInfo.Name*"></a>
  <h4 id="Database_RecipeWeightInfo_Name" data-uid="Database.RecipeWeightInfo.Name">Name</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Name { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the name column. This column is the name of the product to be weighted</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_NextSeqId_" data-uid="Database.RecipeWeightInfo.NextSeqId*"></a>
  <h4 id="Database_RecipeWeightInfo_NextSeqId" data-uid="Database.RecipeWeightInfo.NextSeqId">NextSeqId</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int NextSeqId { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the next sequential id column. This column contains the id (see Id from IComTabInfo) of the row of the next sequential table. From ISeqTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_NextSeqType_" data-uid="Database.RecipeWeightInfo.NextSeqType*"></a>
  <h4 id="Database_RecipeWeightInfo_NextSeqType" data-uid="Database.RecipeWeightInfo.NextSeqType">NextSeqType</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int NextSeqType { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the next sequential type column. The type is a variable used to identify the next sequential table. From ISeqTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_SeqType_" data-uid="Database.RecipeWeightInfo.SeqType*"></a>
  <h4 id="Database_RecipeWeightInfo_SeqType" data-uid="Database.RecipeWeightInfo.SeqType">SeqType</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int SeqType { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Identification number of the current sequential table. From ISeqTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Setpoint_" data-uid="Database.RecipeWeightInfo.Setpoint*"></a>
  <h4 id="Database_RecipeWeightInfo_Setpoint" data-uid="Database.RecipeWeightInfo.Setpoint">Setpoint</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Setpoint { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the setpoings column. This column contains the target weight by unit of final product</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_TabName_" data-uid="Database.RecipeWeightInfo.TabName*"></a>
  <h4 id="Database_RecipeWeightInfo_TabName" data-uid="Database.RecipeWeightInfo.TabName">TabName</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public string TabName { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.String</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Name of the database table. From IBasTabInfo interface</p>
</td>
      </tr>
    </tbody>
  </table>
  
  
  <a id="Database_RecipeWeightInfo_Unit_" data-uid="Database.RecipeWeightInfo.Unit*"></a>
  <h4 id="Database_RecipeWeightInfo_Unit" data-uid="Database.RecipeWeightInfo.Unit">Unit</h4>
  <div class="markdown level1 summary"></div>
  <div class="markdown level1 conceptual"></div>
  <h5 class="decalaration">Declaration</h5>
  <div class="codewrapper">
    <pre><code class="lang-csharp hljs">public int Unit { get; }</code></pre>
  </div>
  <h5 class="propertyValue">Property Value</h5>
  <table class="table table-bordered table-striped table-condensed">
    <thead>
      <tr>
        <th>Type</th>
        <th>Description</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td><span class="xref">System.Int32</span></td>
        <td><p sourcefile="api/Database.RecipeWeightInfo.yml" sourcestartlinenumber="1">Index of the unit column. This column contains the unit of the setpoint, min and max</p>
</td>
      </tr>
    </tbody>
  </table>
  <h3 id="implements">Implements</h3>
  <div>
      <a class="xref" href="Database.ISeqTabInfo.html">ISeqTabInfo</a>
  </div>
  <div>
      <a class="xref" href="Database.IComTabInfo.html">IComTabInfo</a>
  </div>
  <div>
      <a class="xref" href="Database.IBasTabInfo.html">IBasTabInfo</a>
  </div>
</article>
          </div>
          
          <div class="hidden-sm col-md-2" role="complementary">
            <div class="sideaffix">
              <div class="contribution">
                <ul class="nav">
                </ul>
              </div>
              <nav class="bs-docs-sidebar hidden-print hidden-xs hidden-sm affix" id="affix">
                <h5>In This Article</h5>
                <div></div>
              </nav>
            </div>
          </div>
        </div>
      </div>
      
      <footer>
        <div class="grad-bottom"></div>
        <div class="footer">
          <div class="container">
            <span class="pull-right">
              <a href="#top">Back to top</a>
            </span>
            
            <span>Generated by <strong>DocFX</strong></span>
          </div>
        </div>
      </footer>
    </div>
    
    <script type="text/javascript" src="../styles/docfx.vendor.js"></script>
    <script type="text/javascript" src="../styles/docfx.js"></script>
    <script type="text/javascript" src="../styles/main.js"></script>
  </body>
</html>
