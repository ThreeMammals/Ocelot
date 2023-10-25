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

command=$1
echo Doing $command ...
if [ "$command" == "" ]
then
	status="FAILED"
	echo There is no build command! See Help log below.
	$SPHINXBUILD -M help $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
else
	status="DONE"
	$SPHINXBUILD -M $1 $SOURCEDIR $BUILDDIR $SPHINXOPTS $O
fi
echo Build $status
