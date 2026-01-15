# Command file for Sphinx documentation
# PowerShell version without env var usage

param (
    [string]$command
)

# Define sphinx-build as a normal variable
$sphinxBuild = "sphinx-build"

$SOURCEDIR = "."
$BUILDDIR = "_build"

Write-Host "Doing $command ..."

if ([string]::IsNullOrEmpty($command)) {
    $status = "FAILED"
    Write-Host "There is no build command! Available commands: clean, html"
    Write-Host "See Sphinx Help below."
    & $sphinxBuild -M help $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
} else {
    & $sphinxBuild -M $command $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
    $status = "DONE"
}

Write-Host "Build $status"
