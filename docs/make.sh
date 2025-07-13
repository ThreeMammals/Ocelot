#!/bin/sh
#
# Command file for Sphinx documentation
#
if [ "$SPHINXBUILD" == "" ]
then
   SPHINXBUILD="sphinx-build"
fi

SOURCEDIR="."
BUILDDIR="_build"

command=$1 # html, clean and etc.
echo Doing $command ...
if [ "$command" == "" ]
then
   status="FAILED"
   echo There is no build command! Available commands: clean, html
   echo See Sphinx Help below.
   $SPHINXBUILD -M help $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
else
   $SPHINXBUILD -M $command $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
   status="DONE"
fi
echo Build $status
