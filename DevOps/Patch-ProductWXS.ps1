[xml]$xml = (Get-Content "./Product1.wxs" | ConvertTo-Xml)

Write-Host $xml.Wix.Product.Version
#Write-Host ("$version is {0}" -f $xdoc.Wix.Product.Version )
#$productNode = $xdoc.SelectSingleNode("//Product")

#Write-Host $productNode
#$productNode.UpdateAttribute("Version", "1.2.3");

#Write-Host $node
#Write-Host 'Saving'
#$xdoc.Save(“.\Product1.wxs”)

#$xdoc = [xml] (Get-Content “.\Product1.wxs”)

#Write-Host ("$version is {0}" -f $xdoc.Wix.Product.Version )
Exit-PSSession