<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="/">
   <html>
   <head>
   <style>
		table {
			border-collapse: collapse;
		}

		table, td, th {
			border: 1px solid lightgrey;
		}
   </style>
   </head>
   <body>
   <xsl:apply-templates select="//ModelDoc"/>
   </body>
   </html>
</xsl:template>

<!-- ============================================================================
     Matches all 'ModelDoc' elements.
     ============================================================================ -->
<xsl:template match="ModelDoc">
   <h2>Model <xsl:value-of select="Name"/></h2>
   <h3>Outputs</h3>
   <table>
   <tr>
   <td><b>Name</b></td>
   <td><b>Type Name</b></td>
   <td><b>Units</b></td>
   <td><b>Description</b></td>
   <td><b>Is Writable?</b></td>
   <td><b>Is Field?</b></td>
   </tr>
   <xsl:apply-templates select="Outputs/Output" />
   </table>
   
   <h3>Links</h3>
   <table>
   <tr>
   <td><b>Name</b></td>
   <td><b>Type Name</b></td>
   <td><b>Units</b></td>
   <td><b>Description</b></td>
   <td><b>Linked model name</b></td>
   <td><b>Is optional?</b></td>
   </tr>
   <xsl:apply-templates select="Links/Link" />
   </table>
   
   <h3>Events published</h3>
   <table>
   <tr>
   <td><b>Name</b></td>
   <td><b>Type Name</b></td>
   <td><b>Names of models subscribed to event</b></td>
   </tr>
   <xsl:apply-templates select="Events/Event" />   
   </table>
   
</xsl:template>

<!-- ============================================================================
     Matches all 'Output' elements.
     ============================================================================ -->
<xsl:template match="Output"> 
   <tr>
   <td><xsl:value-of select="Name"/></td>
   <td><xsl:value-of select="TypeName"/></td>
   <td><xsl:value-of select="Units"/></td>
   <td><xsl:value-of select="Description"/></td>
   <td><xsl:value-of select="IsWritable"/></td>
   <td><xsl:value-of select="IsField"/></td>
   </tr>
</xsl:template>


<!-- ============================================================================
     Matches all 'Link' elements.
     ============================================================================ -->
<xsl:template match="Link"> 
   <tr>
   <td><xsl:value-of select="Name"/></td>
   <td><xsl:value-of select="TypeName"/></td>
   <td><xsl:value-of select="Units"/></td>
   <td><xsl:value-of select="Description"/></td>
   <td><xsl:value-of select="LinkedModelName"/></td>
   <td><xsl:value-of select="IsOptional"/></td>
   </tr>
</xsl:template>



<!-- ============================================================================
     Matches all 'Event' elements.
     ============================================================================ -->
<xsl:template match="Event"> 
   <tr>
   <td><xsl:value-of select="Name"/></td>
   <td><xsl:value-of select="TypeName"/></td>
   <td>
      <xsl:apply-templates select="SubscriberName" />
   </td>
   </tr>
</xsl:template>



<!-- ============================================================================
     Matches all 'SubscriberName' elements.
     ============================================================================ -->
<xsl:template match="SubscriberName"> 
   <tr>
     <td />
     <td />
     <td><xsl:value-of select="."/></td></tr>
</xsl:template> 

</xsl:stylesheet>
