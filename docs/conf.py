# Configuration file for the Sphinx documentation builder.
#
# For the full list of built-in configuration values, see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Project information -----------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#project-information

project = 'Ocelot Gateway'
copyright = ' 2016-%Y, ThreeMammals Ocelot team'
author = 'Tom Pallister, Raman Maksimchuk'
release = '23.3'

# -- General configuration ---------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#general-configuration

extensions = [
    'sphinx.ext.autodoc',
    'sphinx.ext.doctest',
    'sphinx.ext.todo',
    'sphinx.ext.autosummary',
    'sphinx.ext.extlinks',
    'sphinx.ext.intersphinx',
    'sphinx.ext.viewcode',
    'sphinx.ext.inheritance_diagram',
    'sphinx.ext.coverage',
    'sphinx.ext.graphviz',
]

templates_path = ['_templates']
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store']

# -- Options for HTML output -------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#options-for-html-output

# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_theme
html_theme = 'alabaster'

# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_static_path
html_static_path = ['_static']
html_css_files = ['overrides.css']
